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

        public async Task<ParsedRecipeDto> ParseAsync(string ocrText)
        {
            var prompt = RecipePromptBuilder.Build(ocrText);

            _logger.LogInformation("===== OCR TEXT SENT TO LLM =====");
            _logger.LogInformation("{OcrText}", ocrText);
            _logger.LogInformation("================================");


            // 1. Appel LLM (réponse brute)
            var raw = await _client.CompleteAsync(prompt);

            _logger.LogInformation("===== RAW LLM RESPONSE =====");
            _logger.LogInformation("{RawResponse}", raw);
            _logger.LogInformation("============================");


            // 2. Extraction du JSON strict
            var json = ExtractJson(raw);

            // 3. Désérialisation robuste
            var result = JsonSerializer.Deserialize<ParsedRecipeDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result
                ?? throw new InvalidOperationException("Failed to deserialize AI response.");
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
