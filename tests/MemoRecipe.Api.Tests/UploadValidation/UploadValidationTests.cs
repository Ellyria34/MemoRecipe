using Microsoft.AspNetCore.Mvc.Testing;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Net.Http.Headers;
using System.Text;

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
    public async Task UploadFile_WithRejectedExtension_ReturnBadRequest()
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

    [Fact]
    public async Task UploadFile_WithInvalidMime_ReturnBadRequest()
    {
        //Arrange
        await EnsureTestUserAndLoginAsync();

        byte[] fakeBytes = { 0x00, 0x01, 0x02 };
        using var fakeMime = new MemoryStream(fakeBytes);
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fakeMime), "imageFile", "test.jpeg");

        //Act
        var response = await _client.PostAsync("api/recipe/scan", content);

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var bodyContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("MIME", bodyContent);
    }

    [Fact]
    public async Task UploadFile_WithInvalidMagicBytes_ReturnBadRequest()
    {
        //Arrange
        await EnsureTestUserAndLoginAsync();

        byte[] fakeBytes = Encoding.UTF8.GetBytes("this is definitely not an image file");  // 36 octets
        using var fakeStream = new MemoryStream(fakeBytes);
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fakeStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "fake.jpeg");

        //Act
        var response = await _client.PostAsync("api/recipe/scan", content);

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var bodyContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("magic bytes mismatch", bodyContent);
    }

    [Fact]
    public async Task UploadFile_WithValidFile_ReturnOk()
    {
        //Arrange
        await EnsureTestUserAndLoginAsync();

        byte[] jpegSignature = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };  // 10 octets because > 8
        using var fakeStream = new MemoryStream(jpegSignature);
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fakeStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "fake.jpeg");

        //Act
        var response = await _client.PostAsync("api/recipe/scan", content);

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var bodyContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("FakeRecipeTitle", bodyContent);
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