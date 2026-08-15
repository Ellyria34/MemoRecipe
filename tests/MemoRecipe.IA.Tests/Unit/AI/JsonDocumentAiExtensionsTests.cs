using System.Text.Json;
using MemoRecipeIA.Infrastructure.AI;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.AI;

public class JsonDocumentAiExtensionsTests
{
    // ===== ParseOpenAiUsage =====

    [Fact]
    public void ParseOpenAiUsage_WithValidUsageBlock_ReturnsTokens()
    {
        const string json = """
            {
              "choices": [],
              "usage": { "prompt_tokens": 1234, "completion_tokens": 567, "total_tokens": 1801 }
            }
            """;
        using var doc = JsonDocument.Parse(json);

        var (promptTokens, completionTokens) = doc.ParseOpenAiUsage();

        Assert.Equal(1234, promptTokens);
        Assert.Equal(567, completionTokens);
    }

    [Fact]
    public void ParseOpenAiUsage_MissingUsageBlock_ReturnsZeros()
    {
        const string json = """{ "choices": [] }""";
        using var doc = JsonDocument.Parse(json);

        var (promptTokens, completionTokens) = doc.ParseOpenAiUsage();

        Assert.Equal(0, promptTokens);
        Assert.Equal(0, completionTokens);
    }

    // ===== ParseGeminiUsage =====

    [Fact]
    public void ParseGeminiUsage_WithValidUsageMetadata_ReturnsTokens()
    {
        const string json = """
            {
              "candidates": [],
              "usageMetadata": { "promptTokenCount": 4200, "candidatesTokenCount": 350, "totalTokenCount": 4550 }
            }
            """;
        using var doc = JsonDocument.Parse(json);

        var (promptTokens, completionTokens) = doc.ParseGeminiUsage();

        Assert.Equal(4200, promptTokens);
        Assert.Equal(350, completionTokens);
    }

    [Fact]
    public void ParseGeminiUsage_MissingUsageMetadata_ReturnsZeros()
    {
        const string json = """{ "candidates": [] }""";
        using var doc = JsonDocument.Parse(json);

        var (promptTokens, completionTokens) = doc.ParseGeminiUsage();

        Assert.Equal(0, promptTokens);
        Assert.Equal(0, completionTokens);
    }
}