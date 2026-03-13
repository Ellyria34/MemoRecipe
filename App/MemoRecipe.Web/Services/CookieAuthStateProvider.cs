using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;


namespace MemoRecipe.Web.Services;

public class CookieAuthStateProvider : AuthenticationStateProvider  
{
    private AuthenticationState? _cachedState;
    private readonly IHttpClientFactory _httpClientFactory;

    public CookieAuthStateProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        var client = _httpClientFactory.CreateClient("MemoRecipe");
        var response = await client.GetAsync("api/auth/me");

        if(!response.IsSuccessStatusCode)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            _cachedState =  new AuthenticationState(anonymous);
            return _cachedState;
        }
        var claims = new[] { new Claim(ClaimTypes.Name, "utilisateur") };
        var identity = new ClaimsIdentity(claims, "cookie");
        var user = new ClaimsPrincipal(identity);
        _cachedState = new AuthenticationState(user);
        return _cachedState;
    }

    public void NotifyAuthChanged()
    {
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
} 