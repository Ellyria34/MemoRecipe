using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Application.Tests.Fakes;

public class FakeRecipeRepository : IRecipeRepository
{
    private readonly List<Recipe> _recipes = new();

    public Task<Recipe?> GetByIdAsync(Guid id)
    {
        var recipe = _recipes.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(recipe);
    }

    public Task<List<Recipe>> GetAllByUserIdAsync(Guid userId)
    {
        var recipes = _recipes.Where(r => r.UserId == userId).ToList();
        return Task.FromResult(recipes);
    }

    public Task AddAsync(Recipe recipe)
    {
        _recipes.Add(recipe);
        return Task.CompletedTask;
    }

    public void Update(Recipe recipe)
    {
        var index = _recipes.FindIndex(r => r.Id == recipe.Id);
        if (index >= 0)
            _recipes[index] = recipe;
    }

    public void Delete(Recipe recipe)
    {
        _recipes.Remove(recipe);
    }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}
