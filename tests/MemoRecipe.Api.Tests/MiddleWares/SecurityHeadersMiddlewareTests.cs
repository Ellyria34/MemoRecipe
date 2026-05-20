using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace MemoRecipe.Api.Tests.Middlewares;

public class SecurityHeadersMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityHeadersMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Theory]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Permissions-Policy", "camera=(), microphone=(), geolocation=()")]
    [InlineData("Content-Security-Policy", "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'")]
    public async Task Get_AnyEndpoint_ReturnsSecurityHeader(string headerName, string expectedValue)
    {
        // Arrange + Act
        var response = await _client.GetAsync("api/recipe");

        // Assert
        Assert.True(response.Headers.Contains(headerName));
        Assert.Equal(expectedValue, response.Headers.GetValues(headerName).First());
    }

    [Fact]
    public async Task Get_InDevelopment_DoesNotReturnHsts()
    {
        // Arrange + Act
        var response = await _client.GetAsync("api/recipe");

        // Assert
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Get_InProduction_ReturnsHsts()
    {
        // Arrange
        var newClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        }).CreateClient();


        // Act
        var response = await newClient.GetAsync("api/recipe");

        // Assert
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.Equal("max-age=63072000; includeSubDomains; preload", response.Headers.GetValues("Strict-Transport-Security").First());

    }

    // TestServer limitation: this test always passes in-memory because
    // TestServer does not emit the Server header by default (unlike Kestrel).
    // Real verification of Kestrel.AddServerHeader = false must be done manually
    // via DevTools -> Network-> Response Headers in dev/prod
    [Fact]
    public async Task Get_ResponseDoesNotIncludeServerHeader()
    {
        // Arrange + Act
        var response = await _client.GetAsync("api/auth/me");

        // Assert
        Assert.False(response.Headers.Contains("Server"));
    }


}