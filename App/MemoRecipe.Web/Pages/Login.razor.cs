using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using System.Dynamic;
using Microsoft.AspNetCore.Components.Authorization;


namespace MemoRecipe.Web.Pages;

public partial class Login
{
    [Inject]
    private AuthenticationStateProvider AuthStateProvider {get; set;} = default!;

    [Inject]
    private IAuthService AuthService {get; set;} = default!;

    [Inject]
    private NavigationManager Navigation  {get; set;} = default!;

    string _email = string.Empty;
    string _password = string.Empty; 
    bool _showPassword = false;
    string _errorMessage = string.Empty;
    bool _isValid = false;

    private string? ValidateEmail(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "L'email est obligatoire";
        }
        return null;
    }

    private string? ValidatePassword(string value)
    {
        if (string.IsNullOrEmpty(value)) 
        {
            return "Le mot de passe est obligatoire";
        }
        return null;
    }

   private async Task HandleLogin()
    {
        if (!_isValid) return;
        var result = await AuthService.LoginAsync(_email, _password);
        if(!result)
        {
            _errorMessage = "Email ou mot de passe incorrect";
            return;
        }
        ((CookieAuthStateProvider)AuthStateProvider).NotifyAuthChanged();
        Navigation.NavigateTo("/");
    }
}