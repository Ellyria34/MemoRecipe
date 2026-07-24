using System.Net.Http.Json;
using MemoRecipe.Api.Tests.Helpers;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MemoRecipe.Api.Tests.Recipes;

public class RecipeUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RecipeUpdateTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> AuthenticateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        const string email = "recipeUpdateTestUser@test.com";
        const string password = "CorrectPassword1!";

        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = "recipeUpdateTestUser",
                PasswordHash = "",
                PasswordSalt = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            db.SaveChanges();
        }

        await _client.PostAsJsonAsync("api/auth/login", new { email, password });
        return user.Id;
    }

    [Fact]
    public async Task UpdateRecipe_WithNewIngredients_PersistsThem()
    {
        // Arrange : auth + seed recipe with 2 initial ingredients
        var userId = await AuthenticateAsync();
        var recipeId = Guid.NewGuid();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var recipe = new Recipe
            {
                Id = recipeId,
                Title = "Recette initiale",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Ingredients = new List<Ingredient>
            {
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Ancien 1", Quantity = 100, Unit = "g" },
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Ancien 2", Quantity = 200, Unit = "ml" }
            }
            };
            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
        }

        var updateDto = new
        {
            title = "Recette initiale",
            ingredients = new[]
            {
            new { name = "Nouveau A", quantity = 50m, unit = "g" },
            new { name = "Nouveau B", quantity = 75m, unit = "ml" },
            new { name = "Nouveau C", quantity = 1m, unit = "pièce" }
        }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"api/recipe/{recipeId}", updateDto);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert DB : new scope to avoid stale tracked entities
        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var persistedRecipe = await assertDb.Recipes
            .Include(r => r.Ingredients)
            .AsNoTracking()
            .FirstAsync(r => r.Id == recipeId);

        Assert.Equal(3, persistedRecipe.Ingredients.Count);
        Assert.Contains(persistedRecipe.Ingredients, i => i.Name == "Nouveau A");
        Assert.Contains(persistedRecipe.Ingredients, i => i.Name == "Nouveau B");
        Assert.Contains(persistedRecipe.Ingredients, i => i.Name == "Nouveau C");
        Assert.DoesNotContain(persistedRecipe.Ingredients, i => i.Name == "Ancien 1");
        Assert.DoesNotContain(persistedRecipe.Ingredients, i => i.Name == "Ancien 2");
    }

    [Fact]
    public async Task UpdateRecipe_WithFewerIngredients_RemovesOldOnes()
    {
        // Arrange : seed recipe with 3 initial ingredients
        var userId = await AuthenticateAsync();
        var recipeId = Guid.NewGuid();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var recipe = new Recipe
            {
                Id = recipeId,
                Title = "Recette à réduire",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Ingredients = new List<Ingredient>
            {
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Ancien 1", Quantity = 100, Unit = "g" },
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Ancien 2", Quantity = 200, Unit = "ml" },
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Ancien 3", Quantity = 3, Unit = "pièces" }
            }
            };
            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
        }

        var updateDto = new
        {
            title = "Recette à réduire",
            ingredients = new[]
            {
            new { name = "Seul survivant", quantity = 500m, unit = "g" }
        }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"api/recipe/{recipeId}", updateDto);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert DB : exactly 1 ingredient, the 3 old ones removed
        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var persistedRecipe = await assertDb.Recipes
            .Include(r => r.Ingredients)
            .AsNoTracking()
            .FirstAsync(r => r.Id == recipeId);

        Assert.Single(persistedRecipe.Ingredients);
        Assert.Equal("Seul survivant", persistedRecipe.Ingredients.First().Name);
        Assert.DoesNotContain(persistedRecipe.Ingredients, i => i.Name == "Ancien 1");
        Assert.DoesNotContain(persistedRecipe.Ingredients, i => i.Name == "Ancien 2");
        Assert.DoesNotContain(persistedRecipe.Ingredients, i => i.Name == "Ancien 3");
    }

    [Fact]
    public async Task UpdateRecipe_WithReorderedSteps_KeepsOrderIndexCorrect()
    {
        // Arrange : seed recipe with 3 steps in initial order
        var userId = await AuthenticateAsync();
        var recipeId = Guid.NewGuid();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            var recipe = new Recipe
            {
                Id = recipeId,
                Title = "Recette à réordonner",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Steps = new List<Step>
            {
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Order = 1, Instruction = "Instruction A" },
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Order = 2, Instruction = "Instruction B" },
                new() { Id = Guid.NewGuid(), RecipeId = recipeId, Order = 3, Instruction = "Instruction C" }
            }
            };
            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
        }

        // Submit steps in reversed order (C, B, A) : service should recalculate Order to 1, 2, 3
        var updateDto = new
        {
            title = "Recette à réordonner",
            steps = new[]
            {
            new { instruction = "Instruction C" },
            new { instruction = "Instruction B" },
            new { instruction = "Instruction A" }
        }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"api/recipe/{recipeId}", updateDto);

        // Assert HTTP
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert DB : Order recalculated based on position (1, 2, 3)
        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var persistedRecipe = await assertDb.Recipes
            .Include(r => r.Steps)
            .AsNoTracking()
            .FirstAsync(r => r.Id == recipeId);

        Assert.Equal(3, persistedRecipe.Steps.Count);

        // Steps sorted by Order : verify Instruction matches expected position
        var orderedSteps = persistedRecipe.Steps.OrderBy(s => s.Order).ToList();
        Assert.Equal("Instruction C", orderedSteps[0].Instruction);
        Assert.Equal(1, orderedSteps[0].Order);
        Assert.Equal("Instruction B", orderedSteps[1].Instruction);
        Assert.Equal(2, orderedSteps[1].Order);
        Assert.Equal("Instruction A", orderedSteps[2].Instruction);
        Assert.Equal(3, orderedSteps[2].Order);
    }

}
