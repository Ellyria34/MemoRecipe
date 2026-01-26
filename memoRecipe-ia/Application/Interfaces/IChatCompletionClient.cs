namespace MemoRecipeIA.Application.Interfaces
{
    public interface IChatCompletionClient
    {
        /// <summary>
        /// Sends a prompt to a language model and returns the raw textual response.
        /// The response is expected to be a JSON string.
        /// </summary>
        Task<string> CompleteAsync(string prompt);
    }
}