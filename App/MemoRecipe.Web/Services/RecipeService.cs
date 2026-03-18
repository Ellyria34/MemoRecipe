using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public class RecipeService : IRecipeService
{
    private readonly HttpClient _httpClient;

    public RecipeService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MemoRecipe");
    }

    public async Task<ExtractedRecipeDto> ScanImageAsync(Stream imageStream)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StreamContent(imageStream), "imageFile","image.jpg");
        var response = await _httpClient.PostAsync("api/recipe/scan", content);

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ExtractedRecipeDto>(json, options)
            ?? throw new InvalidOperationException("Erreur de désérialisation");
    }

    public async Task<RecipeDto> CreateRecipeAsync(RecipeCreateDto recipeCreateDto)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = JsonSerializer.Serialize(recipeCreateDto, options);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/recipe", content);

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RecipeDto>(json, options)
            ?? throw new InvalidOperationException("Erreur de désérialisation");
    }

}