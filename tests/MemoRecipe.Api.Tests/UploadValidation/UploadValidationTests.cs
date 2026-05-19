using Microsoft.AspNetCore.Mvc.Testing;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace MemoRecipe.Api.Tests.UploadValidation;

public class UploadValidationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UploadValidationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadFile_WithRejectedFileType_ReturnBadRequest()
    {
        //Arrange
        await EnsureTestUserAndLoginAsync();

        byte[] fakeBytes = { 0x00, 0x01, 0x02 };
        using var fakeStream = new MemoryStream(fakeBytes);
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fakeStream), "imageFile", "test.pdf");

        //Act
        var response = await _client.PostAsync("api/recipe/scan", content);

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var bodyContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Extension", bodyContent);

    }

    private async Task EnsureTestUserAndLoginAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        if (!db.Users.Any(u => u.Email == "uploadTestUser@test.com"))
        {

            var user = new User
            {
            Id = Guid.NewGuid(),
            Email = "uploadTestUser@test.com",
            Username = "uploadTestUser",
            PasswordHash = "",       // temporaire, on le remplit juste après
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, "CorrectPassword1!");
            db.Users.Add(user);
            db.SaveChanges();
        }

        await _client.PostAsJsonAsync("api/auth/login", new { email = "uploadTestUser@test.com", password = "CorrectPassword1!" });
    } 
}