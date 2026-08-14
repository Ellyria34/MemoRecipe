namespace MemoRecipeIA.Application.Dtos;

/// <summary>
/// Token usage metadata for an LLM call.
/// Propagated from the pipeline through RecipeDto to the API layer for audit trail (US-A2-04c) and cost tracking (US-A2-05).
/// </summary>
public class AiUsageDto
{
    public string ProviderName { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}
