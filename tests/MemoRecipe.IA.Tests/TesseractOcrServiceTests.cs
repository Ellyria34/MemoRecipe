using System.IO;
using System.Threading.Tasks;
using Xunit;
using memorecipe_ia.Infrastructure.OCR;

namespace MemoRecipe.IA.Tests.Integration.OCR
{
    public class TesseractOcrServiceTests
    {
        [Fact]
        public async Task ExtractAsync_WithValidImage_ReturnsNonEmptyText()
        {
            // Arrange
            var service = new TesseractOcrService();

            var imagePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "test-cheesecake.jpg"
            );

            Assert.True(
                File.Exists(imagePath),
                $"Test image not found at path: {imagePath}"
            );

            await using var stream = File.OpenRead(imagePath);

            // Act
            var extractedText = await service.ExtractAsync(stream);

            // Assert
            Assert.False(
                string.IsNullOrWhiteSpace(extractedText),
                "OCR result should not be empty or whitespace."
            );

            // Assertion volontairement faible mais robuste
            Assert.Contains(
                "cheese",
                extractedText.ToLowerInvariant()
            );
        }
    }
}
