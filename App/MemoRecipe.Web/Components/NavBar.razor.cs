using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;


namespace MemoRecipe.Web.Components;
public partial class NavBar
{
    [Inject]
    private AuthenticationStateProvider AuthStateProvider {get; set;} = default!;

    [Inject]
    private IAuthService AuthService {get; set;} = default!;

    [Inject]
    private NavigationManager Navigation {get; set;} = default!;

    private async Task LogOut()
    {
        await AuthService.LogoutAsync();
        ((CookieAuthStateProvider)AuthStateProvider).NotifyAuthChanged();
        Navigation.NavigateTo("/login");
    }
}