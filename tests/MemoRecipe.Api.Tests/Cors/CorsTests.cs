using Microsoft.AspNetCore.Mvc.Testing;
using MemoRecipe.Api.Tests.Helpers;

namespace MemoRecipe.Api.Tests.Cors;

public class CorsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public CorsTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WithAllowedOrigin_ReturnsAccessControlHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Add("Origin", "http://localhost:5110");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_WithForbiddenOrigin_DoesNotReturnAccessControlHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Add("Origin", "http://evil.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Patch_WithMethodPatch_DoesNotReturnAccessControlHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "api/auth/me");
        request.Headers.Add("Origin", "http://localhost:5110");
        request.Headers.Add("Access-Control-Request-Method", "PATCH");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        if (response.Headers.Contains("Access-Control-Allow-Methods"))
        {
            var methods = response.Headers.GetValues("Access-Control-Allow-Methods").First();
            Assert.DoesNotContain("PATCH", methods);
        }    
    }

}
