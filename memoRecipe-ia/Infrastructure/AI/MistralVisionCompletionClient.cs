using System.Text;
using System.Text.Json;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Infrastructure.AI;

public sealed class MistralVisionCompletionClient : IVisionCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public MistralVisionCompletionClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> CompleteWithImageAsync(string prompt, byte[] dataImage, string mimeType)
    {
        string base64Image = Convert.ToBase64String(dataImage);
        var request = new
        {
            model = "mistral-small-latest",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = $"data:{mimeType};base64,{base64Image}" }
                    }
                }
            },
            temperature = 0.2
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.mistral.ai/v1/chat/completions"
        );

        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Mistral API error {(int)response.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);

        return doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("Empty LLM response");
    }
}
