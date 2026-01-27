using System.Threading.Tasks;
using MemoRecipeIA.Application;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Infrastructure.AI;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.AI
{
    public class NormalizeQuantityTests
    {
        [Theory]
        [InlineData("[15g", "115g")]
        [InlineData("I5g", "I5g")] // ambiguous, left untouched
        [InlineData("l00g", "100g")]
        [InlineData("480ml", "480ml")]
        public void NormalizeQuantity_FixesCommonOcrErrors(string input, string expected)
        {
            var result = OcrQuantityNormalizer.Normalize(input);

            Assert.Equal(expected, result);
        }
    }
}