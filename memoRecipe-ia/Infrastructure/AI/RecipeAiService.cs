using System.Text.Json;
using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;

namespace MemoRecipeIA.Infrastructure.AI
{
    public class RecipeAiService : IRecipeAiService
    {
        private readonly IChatCompletionClient _client;

        public RecipeAiService(IChatCompletionClient client)
        {
            _client = client;
        }

        public async Task<ParsedRecipeDto> ParseAsync(string ocrText)
        {
            var prompt = RecipePromptBuilder.Build(ocrText);

            var json = await _client.CompleteAsync(prompt);

            var result = JsonSerializer.Deserialize<ParsedRecipeDto>(
                json,
                new JsonSerializerOptions
                {
                    // System.Text.Json is case-sensitive by default.
                    // The LLM output casing is not guaranteed, so we enable case-insensitive matching.
                    PropertyNameCaseInsensitive = true
                });

            return result
                ?? throw new InvalidOperationException("Failed to deserialize AI response.");
        }
    }
}
