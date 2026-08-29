using System.Net.Http.Json;
using MemoRecipe.Application.Helpers;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Helpers;

public static class TestUserHelper
{
    public static async Task<Guid> CreateAndLoginAsync(
        CustomWebApplicationFactory<Program> factory,
        HttpClient client,
        string email,
        string password = "CorrectPassword1!",
        string? username = null
    )
    {
        email = EmailNormalizer.Normalize(email);
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = username ?? email.Split('@')[0],
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

        return user.Id;
    }
}