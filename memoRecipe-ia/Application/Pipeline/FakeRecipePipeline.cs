using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Application.Pipeline;

/// <summary>
/// Fake pipeline used in E2E tests (AI_PROVIDER=Fake). Skips OCR (Tesseract) and LLM parsing
/// entirely and returns a hardcoded RecipeDto directly. Avoids the native libleptonica-1.82.0.so
/// dependency in the E2E container image while still exercising the full HTTP request path
/// (browser -> Web -> API -> Function -> response). Not for production use — guarded by the
/// AI_PROVIDER=Fake safety check in Program.cs (throws in Production environment).
/// </summary>
public class FakeRecipePipeline : IRecipePipeline
{
    public Task<RecipeDto> ProcessAsync(Stream imageStream)
    {
        // Stream is intentionally ignored — Fake mode returns the same recipe regardless of input.
        var recipe = new RecipeDto
        {
            Title = "Cheesecake maison",
            Servings = 8,
            Ingredients =
            [
                new IngredientDto { Name = "biscuits emiettes", Quantity = 225m, Unit = "g" },
                new IngredientDto { Name = "jus de citron", Quantity = 2m, Unit = "cas" },
                new IngredientDto { Name = "sucre", Quantity = 100m, Unit = "g" },
                new IngredientDto { Name = "beurre", Quantity = 115m, Unit = "g" },
                new IngredientDto { Name = "creme liquide entiere tres froide", Quantity = 480m, Unit = "ml" },
                new IngredientDto { Name = "fromage frais a temperature ambiante", Quantity = 680m, Unit = "g" }
            ],
            Steps =
            [
                "Melanger les biscuits et le beurre.",
                "Verser et aplatir avec un verre au fond du moule, puis placer au congelateur.",
                "Battre la creme a vitesse moyenne jusqu'a ce qu'elle soit ferme.",
                "Battre separement le fromage frais avec le sucre et le citron jusqu'a ce que ce soit lisse.",
                "Incorporer delicatement la creme au melange.",
                "Verser le tout dans le moule. Couvrir et laisser reposer 6h minimum au frais."
            ],
            AiUsage = new AiUsageDto
            {
                ProviderName = "Fake",
                PromptTokens = 0,
                CompletionTokens = 0
            }
        };

        return Task.FromResult(recipe);
    }
}