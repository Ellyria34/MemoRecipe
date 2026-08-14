using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Infrastructure.AI;

public sealed class MistralChatCompletionClient : IChatCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public MistralChatCompletionClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<LlmCompletionResult> CompleteAsync(string prompt)
    {
        var request = new
        {
            model = "mistral-small-latest",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.mistral.ai/v1/chat/completions"
        );

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.ReadBodyAndEnsureSuccessAsync("Mistral");

        using var doc = JsonDocument.Parse(body);

        var text = doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")            
            .GetString()
            ?? throw new InvalidOperationException("Empty LLM response");

        var (promptTokens, completionTokens) = doc.ParseOpenAiUsage();
        return new LlmCompletionResult(text, promptTokens, completionTokens);

    }
}
