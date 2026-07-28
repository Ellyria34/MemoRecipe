using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthControllerDeleteAccountTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerDeleteAccountTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // DELETE with a JSON body is not supported by the built-in extension methods,
    // so we build the request manually. Same pattern used in AuthValidationSanitizationTests.
    private static HttpRequestMessage BuildDeleteAccountRequest(object payload) =>
        new(HttpMethod.Delete, "api/auth/account")
        {
            Content = JsonContent.Create(payload)
        };

    [Fact]
    public async Task DeleteAccount_WithValidPasswordAndAuth_Returns200AndMarksUserForDeletion()
    {
        // Arrange
        const string email = "delete.valid@test.com";
        const string password = "CorrectPassword1!";
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, email, password);

        // Act
        var request = BuildDeleteAccountRequest(new { password });
        var response = await _client.SendAsync(request);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert DB - user is soft-deleted (DeleteRequestedAt is now set)
        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var user = await assertDb.Users
            .AsNoTracking()
            .FirstAsync(u => u.Email == email);
        Assert.NotNull(user.DeleteRequestedAt);

        // Assert cookie is cleared - subsequent /me returns 401
        var meAfter = await _client.GetAsync("api/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, meAfter.StatusCode);

    }

    [Fact]
    public async Task DeleteAccount_WithWrongPassword_Returns401()
    {
        // Arrange
        const string email = "delete.wrongpwd@test.com";
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, email);

        // Act
        var request = BuildDeleteAccountRequest(new { password = "WrongPassword1!" });
        var response = await _client.SendAsync(request);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

        // Assert DB - user is NOT marked for deletion
        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var user = await assertDb.Users
            .AsNoTracking()
            .FirstAsync(u => u.Email == email);
        Assert.Null(user.DeleteRequestedAt);
                // Regression guard for BACK-085 - the wrong password must not leak in the response body
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("WrongPassword1!", body);
    }

    [Fact]
    public async Task DeleteAccount_WithoutAuth_Returns401()
    {
        // Arrange - fresh anonymous client, no cookie
        var anonymousClient = _factory.CreateClient();

        // Act
        var request = BuildDeleteAccountRequest(new { password = "AnyPassword1!" });
        var response = await anonymousClient.SendAsync(request);

        // Assert - [Authorize] blocks the request before any controller logic runs
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithEmptyPasswordDto_Returns400WithValidationError()
    {
        // Arrange - authentication is required (endpoint is [Authorize])
        // so we log in first, then send an invalid DTO
        const string email = "delete.emptypwd@test.com";
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, email);

        // Act
        var request = BuildDeleteAccountRequest(new { password = "" });
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"propertyName\":\"Password\"", body);
    }
}
