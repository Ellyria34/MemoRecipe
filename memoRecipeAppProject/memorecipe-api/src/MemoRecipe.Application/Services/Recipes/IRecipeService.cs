using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Common;
using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Application.Services.Recipes;

public interface IRecipeService
{
    Task<RecipeDto?> GetByIdAsync(Guid id, Guid userId);
    Task<PagedResult<RecipeDto>> GetAllByUserAsync(Guid userId, RecipeQueryParams queryParams);
    Task<RecipeDto> CreateAsync(RecipeCreateDto dto, Guid userId);
    Task<RecipeDto?> UpdateAsync(Guid id, RecipeUpdateDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<int> CountByUserAsync(Guid userId);
}