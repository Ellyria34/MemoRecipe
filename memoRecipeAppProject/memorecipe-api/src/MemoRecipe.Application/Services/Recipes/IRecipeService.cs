using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Services.Recipes;

public interface IRecipeService
{
    Task<RecipeDto?> GetByIdAsync(Guid id);
    Task<List<RecipeDto>> GetAllByUserAsync(Guid userId);
    Task<RecipeDto> CreateAsync(RecipeCreateDto dto, Guid userId);
    Task<RecipeDto?> UpdateAsync(Guid id, RecipeUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}