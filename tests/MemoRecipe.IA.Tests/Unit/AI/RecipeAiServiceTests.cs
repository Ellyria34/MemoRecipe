using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Infrastructure.AI;
using Xunit;
using MemoRecipeIA.Application.Dtos;

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
            var logger = NullLogger<RecipeAiService>.Instance;

            var service = new RecipeAiService(fakeClient, logger);


            // Act
            var (parsed, _) = await service.ParseAsync(ocrText);

            // Assert
            Assert.Equal("Cheesecake maison", parsed.Title);
            Assert.Equal(8, parsed.Servings);
            Assert.Equal(2, parsed.Ingredients.Count);
            Assert.Equal("biscuits", parsed.Ingredients[0].Name);
            Assert.Equal("225 g", parsed.Ingredients[0].Quantity);
            Assert.Equal(3, parsed.Steps.Count);

        }

        private class FakeChatCompletionClient : IChatCompletionClient
        {
            private readonly string _response;
            public string ProviderName => "Fake";

            public FakeChatCompletionClient(string response)
            {
                _response = response;
            }

            public Task<LlmCompletionResult> CompleteAsync(string prompt)
                => Task.FromResult(new LlmCompletionResult(_response, 0, 0));
        }

    }
}
