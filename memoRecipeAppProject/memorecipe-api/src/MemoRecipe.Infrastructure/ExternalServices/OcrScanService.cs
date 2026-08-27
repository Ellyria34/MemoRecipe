using System.Text.Json;
using MemoRecipe.Application.Services.OcrScan;
using MemoRecipe.Application.DTOs.Recipes;
using Microsoft.Extensions.Configuration;


namespace MemoRecipe.Infrastructure.ExternalServices;

public class OcrScanService : IOcrScanService
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;

    public OcrScanService(IConfiguration configuration,  HttpClient httpClient)
    {
        _baseUrl = configuration["OcrScan:BaseUrl"]
            ?? throw new InvalidOperationException("OcrScan:BaseUrl is missing in configuration");

        var functionKey = configuration["OcrScan:FunctionKey"]
            ?? throw new InvalidOperationException("OcrScan:FunctionKey is missing in configuration");
    
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", functionKey);

    }
    

    public async Task<ExtractedRecipeDto> ProcessImageAsync (Stream stream)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file","image.jpg");
        
        var response = await _httpClient.PostAsync(_baseUrl + "/api/ExtractOcrFunction", content)
            ?? throw new InvalidOperationException("OcrScan:BaseUrl is missing in configuration");
        
        var json = await response.Content.ReadAsStringAsync()
            ?? throw new InvalidOperationException("Failed to deserialize OCR response");
            
        var extractedRecipe = JsonSerializer.Deserialize<ExtractedRecipeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize OCR response");

        return extractedRecipe;
    }
}