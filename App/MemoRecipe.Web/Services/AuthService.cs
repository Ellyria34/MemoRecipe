using System.Text.Json;
using System.Net.Http.Json;

namespace MemoRecipe.Web.Services;
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorageService)
    {
        _httpClient = httpClient;
        _localStorageService = localStorageService;
    }
    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
        
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = result.GetProperty("token").GetString();
        await _localStorageService.SetItemAsync("authToken", token!);
        return true;
    }


    public async Task LogoutAsync()
    {
        await _localStorageService.RemoveItemAsync("authToken");
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorageService.GetItemAsync("authToken");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return string.IsNullOrEmpty(token) ? false : true;
    }

}