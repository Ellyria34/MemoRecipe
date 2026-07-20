using Microsoft.AspNetCore.Mvc.Testing;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;


namespace MemoRecipe.Api.Tests.FeatureFlags;

public class FeatureFlagsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public FeatureFlagsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ScanEndpoint_WhenFeatureDisabled_Returns503()
    {
        // Arrange
        var factoryWithFlagOff = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:ScanRecipeEnabled"] = "false"
                });
            });
        });
        var client = factoryWithFlagOff.CreateClient();
        await AuthenticateAsync(client);
        byte[] fakeBytes = { 0x00 };
        using var fakeStream = new MemoryStream(fakeBytes);
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fakeStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "dummy.jpeg");

        // Act
        var response = await client.PostAsync("api/recipe/scan", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Scan feature disabled", body);
    }


    [Fact]
    public async Task ScanEndpoint_WhenFeatureEnabled_Returns200()
    {
        // Arrange
        var factoryWithFlagOn = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:ScanRecipeEnabled"] = "true"
                });
            });
        });
        var client = factoryWithFlagOn.CreateClient();
        await AuthenticateAsync(client);

        byte[] jpegSignature = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };  // 10 octets because > 8
        using var fakeStream = new MemoryStream(jpegSignature);
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fakeStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "fake.jpeg");

        // Act
        var response = await client.PostAsync("api/recipe/scan", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var bodyContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("FakeRecipeTitle", bodyContent);

    }

    private async Task AuthenticateAsync(HttpClient client)
    {
        // Seed the test user directly in the container's database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        const string email = "featureFlagsTestUser@test.com";
        const string password = "CorrectPassword1!";

        if (!db.Users.Any(u => u.Email == email))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = "featureFlagsTestUser",
                PasswordHash = "",
                PasswordSalt = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            db.SaveChanges();
        }

        // Login via HTTP: sets the auth cookie on the HttpClient's cookie jar
        await client.PostAsJsonAsync("api/auth/login", new { email, password });
    }

}