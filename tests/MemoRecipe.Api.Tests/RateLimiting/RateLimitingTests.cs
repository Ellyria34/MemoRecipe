using Microsoft.AspNetCore.Mvc.Testing;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.RateLimiting;

public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public RateLimitingTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_LoginExceeds10RequetsPerMinutes_Return429()
    {
        
        // Arrange + Act
        for(int i = 0; i < 10 ; i++)
        {
            var content = new StringContent(
                $"{{\"email\":\"test{i}@test.com\",\"password\":\"wrong\"}}",
                System.Text.Encoding.UTF8,
                "application/json");
            await _client.PostAsync("api/auth/login", content);
        }

        // Act : request number 11
        var lastContent = new StringContent(
                """{"email":"test@test.com","password":"wrong"}""",
                System.Text.Encoding.UTF8,
                "application/json");
                var response = await _client.PostAsync("api/auth/login", lastContent);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Post_Login5FailedAttemptsOnSameEmail_Returns429()
    { 
        // Arrange + Act
        for(int i = 0; i < 5 ; i++)
        {
            var content = new StringContent(
                """{"email":"test@test.com","password":"wrong"}""",
                System.Text.Encoding.UTF8,
                "application/json");
            await _client.PostAsync("api/auth/login", content);
        }

        // Act : request number 6
        var lastContent = new StringContent(
                """{"email":"test@test.com","password":"wrong"}""",
                System.Text.Encoding.UTF8,
                "application/json");
                var response = await _client.PostAsync("api/auth/login", lastContent);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Post_LoginSuccessAfterFailures_ResetsCounter()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "reset@test.com",
            Username = "TestUser",
            PasswordHash = "",       // temporaire, on le remplit juste après
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, "CorrectPassword1!");
        db.Users.Add(user);
        db.SaveChanges();
        
        // Act
        for(int i = 0; i < 3 ; i++)
        {
            var content = new StringContent(
                """{"email":"reset@test.com","password":"wrong"}""",
                System.Text.Encoding.UTF8,
                "application/json");
            var response1 = await _client.PostAsync("api/auth/login", content);
        }

        // Act : request reset counter
        var content2 = new StringContent(
                """{"email":"reset@test.com","password":"CorrectPassword1!"}""",
                System.Text.Encoding.UTF8,
                "application/json");

        var response2 = await _client.PostAsync("api/auth/login", content2);

        Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);


        // Act 5 requests failed
        for(int i = 0; i < 5 ; i++)
        {
            var content3 = new StringContent(
                """{"email":"reset@test.com","password":"wrong"}""",
                System.Text.Encoding.UTF8,
                "application/json");
            await _client.PostAsync("api/auth/login", content3);
        }

        // Act 6 requests failed
        var lastContent = new StringContent(
            """{"email":"reset@test.com","password":"wrong"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response3 = await _client.PostAsync("api/auth/login", lastContent);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response3.StatusCode);
    }

}
