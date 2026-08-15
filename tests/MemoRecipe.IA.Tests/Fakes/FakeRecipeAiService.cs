using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipe.IA.Tests.Fakes
{
    public class FakeRecipeAiService : IRecipeAiService
    {
        public Task<(ParsedRecipeDto Parsed, AiUsageDto Usage)> ParseAsync(string ocrText)
        {
            var parsed = new ParsedRecipeDto
            {
                Title = "Cheesecake maison",
                Servings = 8,
                Ingredients =
                {
                    new ParsedIngredientDto
                    {
                        Name = "Biscuits",
                        Quantity = "225 g"
                    }
                },
                Steps =
                {
                    "Mélanger les biscuits et le beurre."
                }
            };

            var usage = new AiUsageDto
            {          
                ProviderName = "Fake",
                PromptTokens = 0,
                CompletionTokens = 0
            };
            return Task.FromResult((parsed, usage));
        }
    }
}