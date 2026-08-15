namespace MemoRecipeIA.Infrastructure.AI;

public static class HttpResponseAiExtensions
{
    /// <summary>
    /// Reads the HTTP response body as string, then throws HttpRequestException
    /// with the body content included if the response is not successful.
    /// This preserves the LLM provider's actual error message (rate limits,
    /// billing issues, quota exceeded, etc.) for diagnosis — where the classic
    /// EnsureSuccessStatusCode() would swallow it.
    /// </summary>
    public static async Task<string> ReadBodyAndEnsureSuccessAsync(
        this HttpResponseMessage response,
        string providerName)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            // Truncate error body to 500 chars to prevent log inflation
            // and reduce leakage surface of provider internals (US-A2-04c)
            var truncated = body.Length > 500
                ? body[..500] + "... [truncated]"
                : body;

            throw new HttpRequestException(
                $"{providerName} API error {(int)response.StatusCode}: {truncated}");
        }
        return body;
    }

}