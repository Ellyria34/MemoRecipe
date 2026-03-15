using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Application.Services.OcrScan;

public interface IOcrScanService
{
    Task<ExtractedRecipeDto> ProcessImageAsync (Stream stream);
}