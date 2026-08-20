using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Recipes;

public class RecipeQuotaTests : IClassFixture<LowQuotaWebApplicationFactory<Program>>
{
    private readonly LowQuotaWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RecipeQuotaTests(LowQuotaWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRecipe_WhenQuotaReached_Returns403()
    {
        // Arrange : auth + seed 2 recipes (= quota max in test config)
        var userId = await TestUserHelper.CreateAndLoginAsync(_factory, _client, "quotaReachedUser@test.com");
        await SeedRecipesAsync(userId, count: 2);

        // Act : try to create 3rd recipe
        var response = await _client.PostAsJsonAsync("/api/recipe", NewRecipeDto("Recette 3"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("recipe_limit_reached", json.GetProperty("error").GetString());
        Assert.Equal(2, json.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task CreateRecipe_WhenBelowQuota_SucceedsUntilLimit()
    {
        // Arrange : auth + seed 1 recipe
        var userId = await TestUserHelper.CreateAndLoginAsync(_factory, _client, "quotaBelowUser@test.com");
        await SeedRecipesAsync(userId, count: 1);

        // Act 1 : 2nd recipe OK (below limit)
        var responseOk = await _client.PostAsJsonAsync("/api/recipe", NewRecipeDto("Recette 2"));

        // Act 2 : 3rd recipe blocked (at limit)
        var responseBlocked = await _client.PostAsJsonAsync("/api/recipe", NewRecipeDto("Recette 3"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, responseOk.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responseBlocked.StatusCode);
    }

    private async Task SeedRecipesAsync(Guid userId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        for (int i = 0; i < count; i++)
        {
            db.Recipes.Add(new Recipe
            {
                Id = Guid.NewGuid(),
                Title = $"Seed recipe {i + 1}",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ScanRecipe_WhenQuotaReached_Returns403()
    {
        // Arrange : auth + seed 2 recipes (= quota max in test config)
        var userId = await TestUserHelper.CreateAndLoginAsync(_factory, _client, "quotaScanUser@test.com");
        await SeedRecipesAsync(userId, count: 2);

        // Act : try to scan when quota reached
        using var multipart = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00 }; // JPEG magic bytes (8 bytes min for ReadExactlyAsync)
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        multipart.Add(imageContent, "imageFile", "test.jpg");

        var response = await _client.PostAsync("/api/recipe/scan", multipart);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("recipe_limit_reached", json.GetProperty("error").GetString());
    }

    private static object NewRecipeDto(string title) => new
    {
        title,
        ingredients = Array.Empty<object>(),
        steps = Array.Empty<object>(),
        categoryIds = Array.Empty<Guid>()
    };
}
