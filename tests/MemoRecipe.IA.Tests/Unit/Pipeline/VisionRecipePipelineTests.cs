using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.Pipeline;

public class VisionRecipePipelineTests
{
    [Fact]
    public async Task ProcessAsync_WithValidVisionResponse_MapsAllFields()
    {
        // Arrange
        var fakeJson = """
            {
              "title": "Poulet au curry",
              "description": "Un plat exotique",
              "servings": 4,
              "prepTimeMinutes": 15,
              "cookTimeMinutes": 25,
              "difficulty": "easy",
              "ingredients": [{ "name": "poulet", "quantity": "450 g" }],
              "steps": ["Couper le poulet", "Cuire à feu doux"]
            }
            """;
        var fakeClient = new FakeVisionCompletionClient(fakeJson);
        var pipeline = new VisionRecipePipeline(fakeClient, NullLogger<VisionRecipePipeline>.Instance);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }); // dummy JPEG bytes

        // Act
        var result = await pipeline.ProcessAsync(stream);

        // Assert: 8 fields mapping
        Assert.Equal("Poulet au curry", result.Title);
        Assert.Equal("Un plat exotique", result.Description);
        Assert.Equal(4, result.Servings);
        Assert.Equal(15, result.PrepTimeMinutes);
        Assert.Equal(25, result.CookTimeMinutes);
        Assert.Equal("easy", result.Difficulty);
        Assert.Equal("15 min", result.PreparationTime);
        Assert.Single(result.Ingredients);
        Assert.Equal(2, result.Steps.Count);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var fakeClient = new FakeVisionCompletionClient("not a json response");
        var pipeline = new VisionRecipePipeline(fakeClient, NullLogger<VisionRecipePipeline>.Instance);
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ProcessAsync(stream));
    }

    // Nested fake (pattern mirrored from RecipeAiServiceTests.cs)
    private class FakeVisionCompletionClient : IVisionCompletionClient
    {
        private readonly string _response;
        public FakeVisionCompletionClient(string response) => _response = response;

        public Task<LlmCompletionResult> CompleteWithImageAsync(string prompt, byte[] imageData, string mimeType)
            => Task.FromResult(new LlmCompletionResult(_response, 0, 0));
    }
}
