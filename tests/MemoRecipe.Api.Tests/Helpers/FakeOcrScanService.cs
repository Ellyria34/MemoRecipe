
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Services.OcrScan;

namespace MemoRecipe.Api.Tests.Helpers;
public class FakeOcrScanService : IOcrScanService
{
    public Task<ExtractedRecipeDto> ProcessImageAsync (Stream stream)
    {
        var fakeRecipe = new ExtractedRecipeDto
        {
            Title = "FakeRecipeTitle",
            Servings = 8,
            PreparationTime = "10",
            Ingredients = new List<string>(){},
            Steps = new List<string>(){},
        };

        return Task.FromResult(fakeRecipe);
    }
}