using System.Text;
using System.Text.Json;
using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Dtos;


namespace MemoRecipeIA.Infrastructure.AI;

public sealed class GeminiChatCompletionClient : IChatCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public string ProviderName => "Gemini";

    public GeminiChatCompletionClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<LlmCompletionResult> CompleteAsync(string prompt)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new []
                    {
                        new
                        {
                            text = prompt
                        },
                    }
                },
            },
            generationConfig = new
            {
                temperature = 0.2
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}"
        );

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.ReadBodyAndEnsureSuccessAsync(ProviderName);

        using var doc = JsonDocument.Parse(body);

        var text = doc
            .RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Empty LLM response");

        var (promptTokens, completionTokens) = doc.ParseGeminiUsage();
        return new LlmCompletionResult(text, promptTokens, completionTokens);

    }
}