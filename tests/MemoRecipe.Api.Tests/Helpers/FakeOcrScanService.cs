
using MemoRecipe.Application.DTOs.Ingredients;
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
            Ingredients = new List<IngredientCreateDto>
            {
                new() { Name = "Farine", Quantity = 200m, Unit = "g" },
                new() { Name = "Sucre", Quantity = 100m, Unit = "g" },
                new() { Name = "Sel", Quantity = null, Unit = null }
            },
            Steps = new List<string>
            {
                "Mélanger la farine et le sucre.",
                "Ajouter une pincée de sel.",
                "Cuire au four 30 minutes."
            },
        };

        return Task.FromResult(fakeRecipe);
    }
}