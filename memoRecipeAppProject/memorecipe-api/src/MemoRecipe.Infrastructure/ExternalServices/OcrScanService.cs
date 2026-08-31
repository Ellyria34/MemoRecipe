using System.Text.Json;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Exceptions;
using MemoRecipe.Application.Services.OcrScan;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace MemoRecipe.Infrastructure.ExternalServices;

public class OcrScanService : IOcrScanService
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrScanService> _logger;

    public OcrScanService(IConfiguration configuration, HttpClient httpClient, ILogger<OcrScanService> logger)
    {
        _baseUrl = configuration["OcrScan:BaseUrl"]
            ?? throw new InvalidOperationException("OcrScan:BaseUrl is missing in configuration");

        var functionKey = configuration["OcrScan:FunctionKey"]
            ?? throw new InvalidOperationException("OcrScan:FunctionKey is missing in configuration");

        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", functionKey);
        _logger = logger;
    }


    public async Task<ExtractedRecipeDto> ProcessImageAsync(Stream stream)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", "image.jpg");

        var response = await _httpClient.PostAsync(_baseUrl + "/api/ExtractOcrFunction", content);

        // Defense in depth: check status before reading body to avoid parsing HTML error pages
        // and to stop the flow early (no wasted token consumption downstream) - US-05 fix
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OcrFunctionUnavailable - Status {StatusCode}", (int)response.StatusCode);
            throw new OcrServiceUnavailableException(response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();

        var extractedRecipe = JsonSerializer.Deserialize<ExtractedRecipeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize OCR response");

        return extractedRecipe;
    }
}
