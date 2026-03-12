using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;

namespace MemoRecipe.Web.Services;
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MemoRecipe");
    }
    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
        
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }

    public async Task<bool> RegisterAsync(string email, string userName, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", new { email, userName, password });
        
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        
        return true;
    }


    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("api/auth/logout", null);
    }


    public async Task<bool> IsAuthenticatedAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/me");

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        return true;
    }
}