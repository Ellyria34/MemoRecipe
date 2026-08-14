using System.Text;
using System.Text.Json;
using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Infrastructure.AI;

public sealed class GeminiVisionCompletionClient : IVisionCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiVisionCompletionClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<LlmCompletionResult> CompleteWithImageAsync(string prompt, byte[] dataImage, string mimeType)
    {
        string base64Image = Convert.ToBase64String(dataImage);
        var request = new
        {
            contents = new[]
                {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Image } }
                    }
                }
            },
            generationConfig = new { temperature = 0.2 }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}"
        );

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.ReadBodyAndEnsureSuccessAsync("GeminiVision");

        using var doc = JsonDocument.Parse(body);

        var text = doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetProperty("parts")[0]            
            .GetString()
            ?? throw new InvalidOperationException("Empty LLM response");

        var (promptTokens, completionTokens) = doc.ParseGeminiUsage();
        return new LlmCompletionResult(text, promptTokens, completionTokens);

    }
}
