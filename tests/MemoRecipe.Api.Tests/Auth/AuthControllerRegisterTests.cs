using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthControllerRegisterTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerRegisterTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidDto_Returns200AndSetsCookie()
    {
        // Arrange
        var payload = new
        {
            email = "register.valid@test.com",
            username = "sanitizeReg",
            password = "CorrectPassword1!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", payload);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        // Assert DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var user = db.Users.FirstOrDefault(u => u.Email == "register.valid@test.com");
        Assert.NotNull(user);
        Assert.Equal("sanitizeReg", user.Username);

    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns400()
    {
        // Arrange
        var payload = new
        {
            email = "register.existing@test.com",
            username = "sanitizeReg",
            password = "CorrectPassword1!"
        };

        await TestUserHelper.CreateAndLoginAsync(_factory, _client, "register.existing@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", payload);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email already exists.", body);
    }

    [Fact]
    public async Task Register_WithInvalidDto_Returns400WithValidationErrors()
    {
        // Arrange
        var payload = new
        {
            email = "register.invalidpayload@test.com",
            username = "sanitizeReg",
            password = "short"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", payload);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"propertyName\":\"Password\"", body);
    }
}