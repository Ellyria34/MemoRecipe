using System.Net.Http.Json; 
using System.Text.Json;
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
        return JsonSerializer.Deserialize<ExtractedRecipeDto>(json) 
            ?? throw new InvalidOperationException("Erreur de désérialisation");
    }
}