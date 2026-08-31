using System.Net;
using System.Net.Http.Headers;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Application.Services.OcrScan;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Recipes;

public class RecipeScanFailureTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public RecipeScanFailureTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // US-05 end-to-end: verifies that when the OCR Function is down (simulated via
    // ThrowingOcrScanService), RecipeController.CreateScannedRecipe catches the typed
    // exception and returns HTTP 503 with a generic FR message (no info leak).
    [Fact]
    public async Task Scan_WhenOcrServiceThrowsUnavailable_Returns503WithGenericFrMessage()
    {
        // Arrange - swap the real IOcrScanService for one that always throws
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOcrScanService));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped<IOcrScanService, ThrowingOcrScanService>();
            });
        }).CreateClient();

        // Login test user (uses the normalized email pattern from P0-8)
        await TestUserHelper.CreateAndLoginAsync(_factory, client, "scan503test@test.com");

        // Prepare a valid multipart image (JPEG magic bytes) to pass all upload gates
        // (extension + MIME + magic bytes) so the flow reaches the OCR call
        var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00 };
        var streamContent = new ByteArrayContent(imageBytes);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "test.jpg");

        // Act
        var response = await client.PostAsync("api/recipe/scan", content);

        // Assert - the full end-to-end contract of US-05
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ocr_unavailable", body);
        Assert.Contains("Service OCR temporairement indisponible", body);
    }
}
