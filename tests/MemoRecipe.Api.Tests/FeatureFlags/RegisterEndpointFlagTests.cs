using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using MemoRecipe.Application.DTOs.Auth;


namespace MemoRecipe.Api.Tests.FeatureFlags;

public class RegisterEndpointFlagTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public RegisterEndpointFlagTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WhenFeatureDisabled_Returns403()
    {
        // Arrange
        var factoryWithFlagOff = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:RegistrationEnabled"] = "false"
                });
            });
        });
        var client = factoryWithFlagOff.CreateClient();
        var dto = new RegisterDto
        {
            Email = "register.flagoff@test.com",
            Username = "flagOffUser",
            Password = "CorrectPassword1!"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/auth/register", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("registration_disabled", body);
    }

    [Fact]
    public async Task Register_WhenFeatureEnabled_Returns200()
    {
        // Arrange
        var factoryWithFlagOn = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:RegistrationEnabled"] = "true"
                });
            });
        });
        var client = factoryWithFlagOn.CreateClient();
        var dto = new RegisterDto
        {
            Email = "register.flagon@test.com",
            Username = "flagOnUser",
            Password = "CorrectPassword1!"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/auth/register", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
