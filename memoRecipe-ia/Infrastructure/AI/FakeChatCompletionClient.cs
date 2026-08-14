using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Infrastructure.AI
{
    public class FakeChatCompletionClient : IChatCompletionClient
    {
        public Task<LlmCompletionResult> CompleteAsync(string prompt)
        {
            const string fakeJson = """
            {
              "title": "Cheesecake maison",
              "servings": 8,
              "ingredients": [
                { "name": "biscuits émiettés", "quantity": "225g" },
                { "name": "jus de citron", "quantity": "2 cas" },
                { "name": "sucre", "quantity": "100g" },
                { "name": "beurre", "quantity": "115g" },
                { "name": "crème liquide entière très froide", "quantity": "480ml" },
                { "name": "fromage frais à température ambiante", "quantity": "680g" }
              ],
              "steps": [
                "Mélanger les biscuits et le beurre.",
                "Verser et aplatir avec un verre au fond du moule, puis placer au congélateur.",
                "Battre la crème à vitesse moyenne jusqu'à ce qu'elle soit ferme.",
                "Battre séparément le fromage frais avec le sucre et le citron jusqu'à ce que ce soit lisse.",
                "Incorporer délicatement la crème au mélange.",
                "Verser le tout dans le moule. Couvrir et laisser reposer 6h minimum au frais."
              ]
            }
            """;

            // Fake: 0 tokens (no real LLM call, no measurement)
            return Task.FromResult(new LlmCompletionResult(fakeJson, 0, 0));
        }
    }
}