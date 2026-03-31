using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public interface IRecipeService
{
    Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream);

    Task<RecipeDto> CreateRecipeAsync(RecipeCreateDto recipeCreateDto);

    Task<List<RecipeDto>> GetAllRecipesAsync(int? limit = null, string? orderBy = null, bool descending = true);

    Task<RecipeDto> GetRecipeByIdAsync(Guid id);

    Task DeleteRecipe(Guid id);

    Task<RecipeDto> UpdateRecipeAsync(Guid id, RecipeUpdateDto updateRecipe);

    Task<int> GetRecipeCountAsync();

}