using MemoRecipeIA.Application.Dtos;

namespace MemoRecipeIA.Application.Interfaces;

public interface IChatCompletionClient
{
    string ProviderName { get; }

    /// <summary>
    /// Sends a prompt to a language model and returns the response including token usage.
    /// The text is expected to be a JSON string.
    /// </summary>
    Task<LlmCompletionResult> CompleteAsync(string prompt);
}
