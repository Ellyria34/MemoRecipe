using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Common;

namespace MemoRecipe.Application.Repositories;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id);
    Task<PagedResult<Recipe>> GetAllByUserIdAsync(Guid userId, RecipeQueryParams queryParams);
    Task AddAsync(Recipe recipe);
    void Update(Recipe recipe);
    void Delete(Recipe recipe);
    Task SaveChangesAsync();
    Task<int> CountByUserAsync(Guid userId);
}