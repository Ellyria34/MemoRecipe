using System.Threading.Tasks;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Infrastructure.AI;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.AI
{
    public class RecipeAiServiceTests
    {
        [Fact]
        public async Task ParseAsync_WithValidJson_ReturnsParsedRecipe()
        {
            // Arrange
            var ocrText = "Cheesecake maison\nPour 8 parts\n225g de biscuits";

            var fakeJsonResponse = """
            {
              "title": "Cheesecake maison",
              "servings": 8,
              "ingredients": [
                { "name": "biscuits", "quantity": "225 g" },
                { "name": "beurre", "quantity": "100 g" }
              ],
              "steps": [
                "Mélanger les biscuits avec le beurre.",
                "Verser dans un moule et tasser.",
                "Laisser reposer au frais."
              ]
            }
            """;

            var fakeClient = new FakeChatCompletionClient(fakeJsonResponse);

            var service = new RecipeAiService(fakeClient);

            // Act
            var result = await service.ParseAsync(ocrText);

            // Assert
            Assert.Equal("Cheesecake maison", result.Title);
            Assert.Equal(8, result.Servings);
            Assert.Equal(2, result.Ingredients.Count);
            Assert.Equal("biscuits", result.Ingredients[0].Name);
            Assert.Equal("225 g", result.Ingredients[0].Quantity);
            Assert.Equal(3, result.Steps.Count);
        }

        private class FakeChatCompletionClient : IChatCompletionClient
        {
            private readonly string _response;

            public FakeChatCompletionClient(string response)
            {
                _response = response;
            }

            public Task<string> CompleteAsync(string prompt)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
