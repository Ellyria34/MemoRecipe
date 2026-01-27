using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Infrastructure.AI
{
    public class FakeChatCompletionClient : IChatCompletionClient
    {
        public Task<string> CompleteAsync(string prompt)
        {
            // Valid minimal JSON for testing purposes
            return Task.FromResult("""
            {
              "title": "Cheesecake maison",
              "servings": 8,
              "ingredients": [
                { "name": "fromage frais", "quantity": "600 g" },
                { "name": "sucre", "quantity": "100 g" }
              ],
              "steps": [
                "Mélanger les ingrédients.",
                "Cuire au four."
              ]
            }
            """);
        }
    }
}
