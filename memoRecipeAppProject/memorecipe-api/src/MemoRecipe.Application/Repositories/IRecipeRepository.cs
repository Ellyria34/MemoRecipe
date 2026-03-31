using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Repositories;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id);
    Task<List<Recipe>> GetAllByUserIdAsync(Guid userId, RecipeQueryParams queryParams);
    Task AddAsync(Recipe recipe);
    void Update(Recipe recipe);
    void Delete(Recipe recipe);
    Task SaveChangesAsync();
    Task<int> CountByUserAsync(Guid userId);
}