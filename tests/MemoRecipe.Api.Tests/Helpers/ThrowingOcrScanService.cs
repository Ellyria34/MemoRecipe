using System.Net;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Exceptions;
using MemoRecipe.Application.Services.OcrScan;

namespace MemoRecipe.Api.Tests.Helpers;

// Fake used to simulate Azure Function OCR down/unavailable in integration tests.
// Always throws OcrServiceUnavailableException, verifying that RecipeController
// catches it and returns 503 with the generic FR message (US-05 end-to-end contract).
public class ThrowingOcrScanService : IOcrScanService
{
    public Task<ExtractedRecipeDto> ProcessImageAsync(Stream stream)
    {
        throw new OcrServiceUnavailableException(HttpStatusCode.InternalServerError);
    }
}