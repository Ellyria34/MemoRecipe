using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public interface IRecipeService
{
    Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream);
}