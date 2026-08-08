using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public interface IRecipeService
{
    Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream, string contentType, string fileName);

    Task<RecipeDto> CreateRecipeAsync(RecipeCreateDto recipeCreateDto);

    Task<PagedResult<RecipeDto>> GetAllRecipesAsync(int page = 1, int pageSize = 10, string? orderBy = null, bool descending = true);

    Task<RecipeDto> GetRecipeByIdAsync(Guid id);

    Task DeleteRecipe(Guid id);

    Task<RecipeDto> UpdateRecipeAsync(Guid id, RecipeUpdateDto updateRecipe);

    Task<int> GetRecipeCountAsync();

}