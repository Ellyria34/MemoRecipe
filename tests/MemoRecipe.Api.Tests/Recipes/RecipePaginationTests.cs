using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Application.DTOs.Common;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Recipes;

public class RecipePaginationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RecipePaginationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecipes_Page1WithSmallerPageSize_ReturnsCorrectPageAndMetadata()
    {
        //Arrange
        var userId = await TestUserHelper.CreateAndLoginAsync(_factory, _client, "paginationTest1@test.com");

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            for (int i = 1; i <= 15; i++)
            {
                db.Recipes.Add(new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = $"Recette {i}",
                    UserId = userId,   // ← variable réutilisée, virgule après
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        //Act
        var response = await _client.GetAsync("api/recipe?page=1&pageSize=10");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<RecipeDto>>();

        //Asert
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetRecipes_PageBeyondTotalPages_ReturnsEmptyItems()
    {
        //Arrange
        var userId = await TestUserHelper.CreateAndLoginAsync(_factory, _client, "paginationTest2@test.com");

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            for (int i = 1; i <= 5; i++)
            {
                db.Recipes.Add(new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = $"Recette {i}",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        //Act and Asert
        var response = await _client.GetAsync("api/recipe?page=999&pageSize=10");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<RecipeDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(999, result.Page);
    }


    [Fact]
    public async Task GetRecipes_PageSizeAbove50_Returns400()
    {
        //Arrange
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, "paginationTest3@test.com");

        //Act and Assert
        var response = await _client.GetAsync("api/recipe?page=1&pageSize=100");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains(problemDetails.Errors.Keys, key =>
            string.Equals(key, "PageSize", StringComparison.OrdinalIgnoreCase));
    }
}
