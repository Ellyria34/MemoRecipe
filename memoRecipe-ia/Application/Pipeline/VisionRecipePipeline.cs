using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MemoRecipeIA.Application.Pipeline;

public class VisionRecipePipeline : IRecipePipeline
{
    private readonly IVisionCompletionClient _visionClient;
    private readonly ILogger<VisionRecipePipeline> _logger;

    public VisionRecipePipeline(IVisionCompletionClient visionClient, ILogger<VisionRecipePipeline> logger)
    {
        _visionClient = visionClient;
        _logger = logger;
    }

    public async Task<RecipeDto> ProcessAsync(Stream imageStream)
    {
        var prompt = RecipePromptBuilder.BuildForVision();

        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        byte[] data = memoryStream.ToArray();

        var raw = await _visionClient.CompleteWithImageAsync(prompt, data, "image/jpeg");

        _logger.LogInformation("===== RAW LLM RESPONSE =====");
        _logger.LogInformation("{RawResponse}", raw.Text);
        _logger.LogInformation("============================");

        // Extract strict JSON from response
        var json = ExtractJson(raw.Text);

        var parsed = JsonSerializer.Deserialize<ParsedRecipeDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to deserialize AI Vision response.");

        var usage = new AiUsageDto
        {
            ProviderName = _visionClient.ProviderName,
            PromptTokens = raw.PromptTokens,
            CompletionTokens = raw.CompletionTokens
        };

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

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start == -1 || end == -1 || end <= start)
            throw new InvalidOperationException("No valid JSON found in LLM response");

        return text.Substring(start, end - start + 1);
    }
}