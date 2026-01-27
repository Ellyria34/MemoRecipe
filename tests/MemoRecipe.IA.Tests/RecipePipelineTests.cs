using System.IO;
using System.Threading.Tasks;
using Xunit;
using MemoRecipeIA.Application.Pipeline;
using MemoRecipeIA.Infrastructure.OCR;
using MemoRecipe.IA.Tests.Fakes;

namespace MemoRecipe.IA.Tests.Integration.Pipeline
{
    public class RecipePipelineTests
    {
        [Fact]
        public async Task ProcessAsync_WithValidImage_ReturnsParsedRecipe()
        {
            // Arrange
            var ocrService = new TesseractOcrService();
            var aiService = new FakeRecipeAiService();

            var pipeline = new RecipePipeline(ocrService, aiService);

            var imagePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "test-cheesecake.jpg"
            );

            Assert.True(File.Exists(imagePath));

            await using var stream = File.OpenRead(imagePath);

            // Act
            var result = await pipeline.ProcessAsync(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cheesecake maison", result.Title);
            Assert.NotEmpty(result.Ingredients);
            Assert.NotEmpty(result.Steps);
        }
    }
}