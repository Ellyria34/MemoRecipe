using System.Text.Json;
using Microsoft.Extensions.Logging;
using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;

namespace MemoRecipeIA.Infrastructure.AI
{
    public class RecipeAiService : IRecipeAiService
    {
        private readonly IChatCompletionClient _client;
        private readonly ILogger<RecipeAiService> _logger;

        public RecipeAiService(IChatCompletionClient client, ILogger<RecipeAiService> logger)
        {
            _client = client;
            _logger = logger;

        }

        public async Task<(ParsedRecipeDto Parsed, AiUsageDto Usage)> ParseAsync(string ocrText)
        {
            var prompt = RecipePromptBuilder.BuildForText(ocrText);

            _logger.LogInformation("OCR text sent to LLM: length={Length} chars", ocrText.Length);

            // 1. Appel LLM (réponse brute)
            var raw = await _client.CompleteAsync(prompt);

            _logger.LogInformation("LLM response received: length={Length} chars", raw.Text.Length);

            // 2. Extraction du JSON strict
            var json = ExtractJson(raw.Text);

            // 3. Désérialisation robuste
            var parsed = JsonSerializer.Deserialize<ParsedRecipeDto>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Failed to deserialize AI response.");

            var usage = new AiUsageDto
            {
                ProviderName = _client.ProviderName,
                PromptTokens = raw.PromptTokens,
                CompletionTokens = raw.CompletionTokens
            };

            return (parsed, usage);
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
}
