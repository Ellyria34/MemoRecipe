using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Security;

namespace MemoRecipeIA.Application.Pipeline;

public class RecipePipeline : IRecipePipeline
{
    private readonly IOcrService _ocrService;
    private readonly IRecipeAiService _recipeAiService;

    public RecipePipeline(
        IOcrService ocrService,
        IRecipeAiService recipeAiService)
    {
        _ocrService = ocrService;
        _recipeAiService = recipeAiService;
    }

    public async Task<RecipeDto> ProcessAsync(Stream imageStream)
    {
        // Step 1: OCR
        var rawText = await _ocrService.ExtractAsync(imageStream);

        // Step 2: Anti prompt-injection sanitize (OWASP LLM01) before sending to LLM
        PromptSanitizer.Sanitize(rawText);

        // Step 3: Parsing IA
        var (parsed, usage) = await _recipeAiService.ParseAsync(rawText);

        // Step 4: Mapping Parsed → RecipeDto
        return new RecipeDto
        {
            Title = parsed.Title,
            Description = parsed.Description,
            Servings = parsed.Servings,
            PrepTimeMinutes = parsed.PrepTimeMinutes,
            CookTimeMinutes = parsed.CookTimeMinutes,
            PreparationTime = parsed.PrepTimeMinutes.HasValue
                ? $"{parsed.PrepTimeMinutes} min"
                : null,
            Difficulty = parsed.Difficulty,
            Ingredients = parsed.Ingredients
                .Select(i =>
                {
                    var quantity = OcrQuantityNormalizer.Normalize(i.Quantity);
                    return string.IsNullOrWhiteSpace(quantity)
                        ? i.Name
                        : $"{quantity} {i.Name}";
                })
                .ToList(),
            Steps = parsed.Steps,
            AiUsage = usage
        };
    }
}