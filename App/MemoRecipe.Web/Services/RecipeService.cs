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

    public async Task<List<RecipeDto>> GetAllRecipesAsync(int? limit = null, string? orderBy = null, bool descending = true)
    {
        var queryParams = new List<string>();
        if (limit.HasValue) queryParams.Add($"limit={limit}");
        if (orderBy != null) queryParams.Add($"orderBy={orderBy}");
        if (!descending) queryParams.Add("descending=false");

        var querystring= string.Join("&", queryParams);

        var response = await _httpClient.GetAsync($"api/recipe?{querystring}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<List<RecipeDto>>(json);
    }

    public async Task<RecipeDto> GetRecipeByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/Recipe/{id}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<RecipeDto>(json);
    }

    public async Task DeleteRecipe(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/recipe/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<RecipeDto> UpdateRecipeAsync(Guid id, RecipeUpdateDto recipeUpdateDto)
    {
        var jsonString = JsonSerializer.Serialize(recipeUpdateDto, _jsonOptions);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"api/recipe/{id}", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<RecipeDto>(json);
    }

    public async Task<int> GetRecipeCountAsync()
    {
        var response = await _httpClient.GetAsync("api/recipe/count");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<int>(json);
    }

}