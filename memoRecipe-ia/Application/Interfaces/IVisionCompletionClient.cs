namespace MemoRecipeIA.Application.Interfaces;

public interface IVisionCompletionClient
{
    /// <summary>
    /// Sends a prompt together with an image to a multimodal language model
    /// and returns the raw textual response. The response is expected to be a JSON string.
    /// </summary>
    /// <param name="prompt">The instruction text sent to the model (system + user message).</param>
    /// <param name="imageData">The raw bytes of the image to analyze.</param>
    /// <param name="mimeType">The MIME type of the image (e.g., "image/jpeg", "image/png", "image/webp").</param>
    Task<string> CompleteWithImageAsync(string prompt, byte[] imageData, string mimeType);
}