using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public interface IRecipeService
{
    Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream);

    Task<RecipeDto> CreateRecipeAsync(RecipeCreateDto recipeCreateDto);

    Task<List<RecipeDto>> GetAllRecipesAsync();

    Task<RecipeDto> GetRecipeByIdAsync(Guid id);

    Task DeleteRecipe(Guid id);

}