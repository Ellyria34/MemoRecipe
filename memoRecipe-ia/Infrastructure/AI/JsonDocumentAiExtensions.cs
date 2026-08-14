using System.Text.Json;

namespace MemoRecipeIA.Infrastructure.AI;

public static class JsonDocumentAiExtensions
{
    /// <summary>
    /// Parses OpenAI-compatible usage block (Groq, Mistral, OpenAI, Anthropic).
    /// Expected shape: { "usage": { "prompt_tokens": 1234, "completion_tokens": 567, "total_tokens": 1801 } }
    /// </summary>
    public static (int PromptTokens, int CompletionTokens) ParseOpenAiUsage(this JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("usage", out var usage))
            return (0, 0);

        var prompt = usage.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
        var completion = usage.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;
        return (prompt, completion);
    }

    /// <summary>
    /// Parses Gemini-format usage metadata block.
    /// Expected shape: { "usageMetadata": { "promptTokenCount": 1234, "candidatesTokenCount": 567, "totalTokenCount": 1801 } }
    /// </summary>
    public static (int PromptTokens, int CompletionTokens) ParseGeminiUsage(this JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("usageMetadata", out var usage))
            return (0, 0);

        var prompt = usage.TryGetProperty("promptTokenCount", out var p) ? p.GetInt32() : 0;
        var completion = usage.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : 0;
        return (prompt, completion);
    }
}