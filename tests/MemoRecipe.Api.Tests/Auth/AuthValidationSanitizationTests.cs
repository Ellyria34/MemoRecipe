using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthValidationSanitizationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // Unique password that would fail validation (no digit) AND is distinctive enough
    // to be safely searched in the response body without false positives.
    private const string LeakedPasswordMarker = "LEAK_TEST_MARKER_xyz";

    public AuthValidationSanitizationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ResponseBodyDoesNotContainPassword()
    {
        // Arrange
        var payload = new
        {
            email = "sanitize.register@test.com",
            username = "sanitizeReg",
            password = LeakedPasswordMarker
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", payload);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(LeakedPasswordMarker, body);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_AttemptedValueIsSanitized()
    {
        // With sanitizer: Password's AttemptedValue is null (either omitted or explicit null in JSON).
        // Without sanitizer: it would be "" (empty string echoing what user submitted).
        var payload = new { email = "sanitize.login@test.com", password = "" };

        var response = await _client.PostAsJsonAsync("api/auth/login", payload);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"propertyName\":\"Password\"", body);
        // Regression guard: without sanitizer, Password's AttemptedValue would be echoed as empty string.
        Assert.DoesNotContain("\"attemptedValue\":\"\"", body);
    }

    [Fact]
    public async Task DeleteAccount_WithEmptyPassword_AttemptedValueIsSanitized()
    {
        await AuthenticateAsync(_client);

        var payload = new { password = "" };
        var request = new HttpRequestMessage(HttpMethod.Delete, "api/auth/account")
        {
            Content = JsonContent.Create(payload)
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"propertyName\":\"Password\"", body);
        Assert.DoesNotContain("\"attemptedValue\":\"\"", body);
    }

    private async Task AuthenticateAsync(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        const string email = "sanitize.deleteauth@test.com";
        const string password = "CorrectPassword1!";

        if (!db.Users.Any(u => u.Email == email))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = "sanitizeDeleteUser",
                PasswordHash = "",
                PasswordSalt = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            db.SaveChanges();
        }

        await client.PostAsJsonAsync("api/auth/login", new { email, password });
    }
}