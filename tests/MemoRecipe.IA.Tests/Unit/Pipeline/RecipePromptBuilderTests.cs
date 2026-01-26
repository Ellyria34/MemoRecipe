using MemoRecipeIA.Application.Pipeline;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.Pipeline
{
    public class RecipePromptBuilderTests
    {
        [Fact]
        public void Build_WithOcrText_IncludesOcrTextAndJsonSchema()
        {
            // Arrange
            var ocrText = "Cheesecake maison\nPour 8 parts\n225g de biscuits";

            // Act
            var prompt = RecipePromptBuilder.Build(ocrText);

            // Assert
            Assert.Contains("Cheesecake maison", prompt);
            Assert.Contains("\"ingredients\": [", prompt);
            Assert.Contains("\"steps\": [ string ]", prompt);
            Assert.Contains("Return ONLY raw JSON", prompt);
        }
    }
}
