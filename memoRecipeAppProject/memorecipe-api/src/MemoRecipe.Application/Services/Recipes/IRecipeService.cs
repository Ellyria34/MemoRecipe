using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Services.Recipes;

public interface IRecipeService
{
    Task<RecipeDto?> GetByIdAsync(Guid id, Guid userId);
    Task<List<RecipeDto>> GetAllByUserAsync(Guid userId);
    Task<RecipeDto> CreateAsync(RecipeCreateDto dto, Guid userId);
    Task<RecipeDto?> UpdateAsync(Guid id, RecipeUpdateDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
}