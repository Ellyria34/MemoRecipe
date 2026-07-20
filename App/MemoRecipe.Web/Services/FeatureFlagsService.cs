using System.Text.Json;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public class FeatureFlagsService : IFeatureFlagsService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private FeatureFlagsDto? _cached;

    public FeatureFlagsService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MemoRecipe");
    }

    public async Task<FeatureFlagsDto> GetAsync()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var response = await _httpClient.GetAsync("api/config/features");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        _cached = JsonSerializer.Deserialize<FeatureFlagsDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Erreur de désérialisation des feature flags");

        return _cached;
    }
}
