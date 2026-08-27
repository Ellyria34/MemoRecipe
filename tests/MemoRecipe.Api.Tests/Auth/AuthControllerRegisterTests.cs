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

        // Assert cookie attributes (HttpOnly + SameSite=Strict for XSS/CSRF protection)
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var authCookieHeader = setCookieHeaders.First(h => h.StartsWith("authCookie=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("httponly", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookieHeader, StringComparison.OrdinalIgnoreCase);

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

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400WithEmailValidationError()
    {
        // Arrange
        var payload = new
        {
            email = "not-an-email",
            username = "sanitizeReg",
            password = "CorrectPassword1!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", payload);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"propertyName\":\"Email\"", body);
    }

    [Fact]
    public async Task Register_WithSameEmailDifferentCase_Returns400_AndStoresLowercase()
    {
        // Arrange — register with mixed case
        var firstPayload = new
        {
            email = "P08.CaseTest@test.com",
            username = "caseTest1",
            password = "CorrectPassword1!"
        };

        var firstResponse = await _client.PostAsJsonAsync("api/auth/register", firstPayload);
        Assert.Equal(System.Net.HttpStatusCode.OK, firstResponse.StatusCode);

        // Assert DB — email stored in lowercase
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var storedUser = db.Users.FirstOrDefault(u => u.Email == "p08.casetest@test.com");
            Assert.NotNull(storedUser);
        }

        // Act — try to register again with UPPERCASE version of the same email
        var secondPayload = new
        {
            email = "P08.CASETEST@test.com",
            username = "caseTest2",
            password = "CorrectPassword1!"
        };
        var secondResponse = await _client.PostAsJsonAsync("api/auth/register", secondPayload);

        // Assert — case-insensitive conflict detected
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("Email already exists.", body);
    }

}