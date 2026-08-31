using System.Net;
using System.Text;
using MemoRecipe.Application.Exceptions;
using MemoRecipe.Infrastructure.ExternalServices;
using MemoRecipe.Infrastructure.Tests.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoRecipe.Infrastructure.Tests.ExternalServices;

public class OcrScanServiceTests
{
    // Test 1 — Verifies that the 'x-functions-key' header is set on the outgoing request
    // to the Azure Function (required by AuthorizationLevel.Function on the Function side).
    // This is THE central criterion of P0-3: without this header, the Function returned
    // 401 Unauthorized in production, silently leaking LLM tokens on API retries.
    [Fact]
    public async Task ProcessImageAsync_AddsXFunctionsKeyHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Empty JSON = default ExtractedRecipeDto instance, no deserialize exception
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        });
        var sut = CreateSut(handler);
        var fakeImage = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // minimal fake JPEG magic bytes

        // Act
        await sut.ProcessImageAsync(fakeImage);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("x-functions-key"),
            "Header 'x-functions-key' must be present on the outgoing request to the Azure Function");
        Assert.Equal("fake-function-key",
            capturedRequest.Headers.GetValues("x-functions-key").First());
    }

    // Test 2 — Verifies fail-fast when OcrScan:FunctionKey config is missing.
    // This does not replace the RequireConfig check in Program.cs, it is a defense-in-depth
    // double-check at the constructor level: if config is corrupted or if a future refactor
    // ever bypasses the Program.cs check, the service still refuses to start silently.
    [Fact]
    public void Constructor_ThrowsWhenFunctionKeyMissing()
    {
        // Arrange
        var httpClient = new HttpClient(new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var configWithoutFunctionKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OcrScan:BaseUrl"] = "https://fake-function.azurewebsites.net"
                // OcrScan:FunctionKey deliberately missing
            })
            .Build();

        // Act + Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new OcrScanService(configWithoutFunctionKey, httpClient, NullLogger<OcrScanService>.Instance));


        Assert.Contains("OcrScan:FunctionKey", exception.Message);
    }

    // Test 3 — US-05: verifies that a non-success status from the Function throws
    // OcrServiceUnavailableException instead of parsing an HTML error body as JSON
    // (which would throw JsonException → 500 to client). The controller catches this
    // typed exception → returns 503 with a generic FR message. Security: no HTML body
    // leaked in logs (just the status code).
    [Fact]
    public async Task ProcessImageAsync_WhenFunctionReturns500_ThrowsOcrServiceUnavailableException()
    {
        // Arrange - simulate Azure gateway 500 with HTML error body (worst case scenario)
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("<html>Azure gateway error</html>", Encoding.UTF8, "text/html")
            }));
        var sut = CreateSut(handler);
        var fakeImage = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        // Act + Assert
        var exception = await Assert.ThrowsAsync<OcrServiceUnavailableException>(
            () => sut.ProcessImageAsync(fakeImage));
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    // ---------- Helpers ----------

    private static OcrScanService CreateSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OcrScan:BaseUrl"] = "https://fake-function.azurewebsites.net",
                ["OcrScan:FunctionKey"] = "fake-function-key"
            })
            .Build();
        return new OcrScanService(config, httpClient, NullLogger<OcrScanService>.Instance);
    }



}
