using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public class RecipeService : IRecipeService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RecipeService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MemoRecipe");
    }

    private T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Erreur de désérialisation");
    }


    public async Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StreamContent(imageStream), "imageFile","image.jpg");
        var response = await _httpClient.PostAsync("api/recipe/scan", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<ExtractedRecipeDto>(json);
    }

    public async Task<RecipeDto> CreateRecipeAsync(RecipeCreateDto recipeCreateDto)
    {
        var jsonString = JsonSerializer.Serialize(recipeCreateDto, _jsonOptions);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/recipe", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<RecipeDto>(json);
    }

    public async Task<List<RecipeDto>> GetAllRecipesAsync()
    {
        var response = await _httpClient.GetAsync("api/recipe");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<List<RecipeDto>>(json);
    }
}