using System.Net;
using System.Text;
using MemoRecipe.Infrastructure.ExternalServices;
using MemoRecipe.Infrastructure.Tests.Notifications;
using Microsoft.Extensions.Configuration;

namespace MemoRecipe.Infrastructure.Tests.ExternalServices;

public class OcrScanServiceTests
{
    // Test 1 — Vérifie que le header 'x-functions-key' est bien ajoute sur la requete sortante
    // vers la Function Azure (protection AuthorizationLevel.Function cote Function).
    // C'est LE critere central de P0-3 : sans ce header, la Function retournait 401 en prod.
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
                // Empty JSON = ExtractedRecipeDto default instance, no deserialize exception
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

    // Test 2 — Verifie le fail-fast si la config OcrScan:FunctionKey est absente
    // Coherent avec le pattern DEC-023 (fail loud early, no silent fallback).
    // Ne remplace pas le RequireConfig cote Program.cs, c'est une double protection
    // defensive au niveau constructeur (defense in depth).
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
            new OcrScanService(configWithoutFunctionKey, httpClient));

        Assert.Contains("OcrScan:FunctionKey", exception.Message);
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
        return new OcrScanService(config, httpClient);
    }
}
