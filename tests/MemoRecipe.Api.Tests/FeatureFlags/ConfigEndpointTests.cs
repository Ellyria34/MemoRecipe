using MemoRecipe.Api.Tests.Helpers;
using Microsoft.Extensions.Configuration;


namespace MemoRecipe.Api.Tests.FeatureFlags;

public class ConfigEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public ConfigEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    [Fact]
    public async Task GetFeatures_WhenScanDisabled_ReturnsFalse()
    {
        // Arrange — override the flag to false via in-memory config
        var factoryWithFlagOff = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:ScanRecipeEnabled"] = "false"
                });
            });
        });
        var client = factoryWithFlagOff.CreateClient();
        // No AuthenticateAsync: the endpoint must be public.

        // Act
        var response = await client.GetAsync("api/config/features");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"scanRecipeEnabled\":false", body);
    }

    [Fact]
    public async Task GetFeatures_WhenScanEnabled_ReturnsTrue()
    {
        // Arrange — override the flag to true via in-memory config
        var factoryWithFlagOn = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:ScanRecipeEnabled"] = "true"
                });
            });
        });
        var client = factoryWithFlagOn.CreateClient();
        // No AuthenticateAsync: the endpoint must be public.

        // Act
        var response = await client.GetAsync("api/config/features");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"scanRecipeEnabled\":true", body);
    }
}