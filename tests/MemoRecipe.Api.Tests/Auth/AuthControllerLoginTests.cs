using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthControllerLoginTests : IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    private readonly NoRateLimitApplicationFactory<Program> _factory;

    public AuthControllerLoginTests(NoRateLimitApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndSetsCookie()
    {
        const string email = "login.correct@test.com";
        const string password = "CorrectPassword1!";

        using var setupClient = _factory.CreateClient();
        await TestUserHelper.CreateAndLoginAsync(_factory, setupClient, email, password);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        // Assert cookie attributes (HttpOnly + SameSite=Strict for XSS/CSRF protection)
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var authCookieHeader = setCookieHeaders.First(h => h.StartsWith("authCookie=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("httponly", authCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookieHeader, StringComparison.OrdinalIgnoreCase);

    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        const string email = "login.wrongpwd@test.com";

        using var setupClient = _factory.CreateClient();
        await TestUserHelper.CreateAndLoginAsync(_factory, setupClient, email);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { email, password = "WrongPassword1!" });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        // the wrong password must not leak in the response body
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("WrongPassword1!", body);

    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            email = "login.doesnotexist@test.com",
            password = "AnyPassword1!"
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithAccountMarkedForDeletion_CancelsDeletionAndReturnsToken()
    {
        const string email = "login.markedfordelete@test.com";
        const string password = "CorrectPassword1!";

        using (var setupClient = _factory.CreateClient())
        {
            await TestUserHelper.CreateAndLoginAsync(_factory, setupClient, email, password);
        }

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var user = db.Users.First(u => u.Email == email);
            user.DeleteRequestedAt = DateTime.UtcNow.AddDays(-15);
            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var refreshedUser = await assertDb.Users
            .AsNoTracking()
            .FirstAsync(u => u.Email == email);
        Assert.Null(refreshedUser.DeleteRequestedAt);
    }

    [Fact]
    public async Task Login_WithAccountExpiredBeyond30Days_PurgesAccountAndReturns401()
    {
        const string email = "login.expired@test.com";
        const string password = "CorrectPassword1!";

        using (var setupClient = _factory.CreateClient())
        {
            await TestUserHelper.CreateAndLoginAsync(_factory, setupClient, email, password);
        }

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var user = db.Users.First(u => u.Email == email);
            user.DeleteRequestedAt = DateTime.UtcNow.AddDays(-31);
            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var purgedUser = await assertDb.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        Assert.Null(purgedUser);
    }

    [Fact]
    public async Task Login_WithLockedOutAccount_Returns429()
    {
        const string email = "login.lockedout@test.com";
        const string password = "CorrectPassword1!";

        using var setupClient = _factory.CreateClient();
        await TestUserHelper.CreateAndLoginAsync(_factory, setupClient, email, password);

        var client = _factory.CreateClient();
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("api/auth/login", new { email, password = "WrongPassword1!" });
        }

        var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });

        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithDifferentCaseThanRegistered_Returns200()
    {
        // Arrange 
        const string email = "Login.CaseTest@test.com";
        const string password = "CorrectPassword1!";

        var setupClient = _factory.CreateClient();
        var registerResponse = await setupClient.PostAsJsonAsync("api/auth/register", new
        {
            email,
            username = "caseTestUser",
            password
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, registerResponse.StatusCode);

        //Act
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { email = "login.casetest@test.com", password = password });

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

}
