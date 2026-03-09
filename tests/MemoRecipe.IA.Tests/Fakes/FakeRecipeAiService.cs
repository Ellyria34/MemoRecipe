using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipe.IA.Tests.Fakes
{
    public class FakeRecipeAiService : IRecipeAiService
    {
        public Task<ParsedRecipeDto> ParseAsync(string ocrText)
        {
            return Task.FromResult(new ParsedRecipeDto
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
            });
        }
    }
}