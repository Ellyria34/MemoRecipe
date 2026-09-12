using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Services;
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
    
    [Inject]
    private IFeatureFlagsService FeatureFlags { get; set; } = default!;

    [Inject]
    private ILogger<Login> Logger { get; set; } = default!;

    string _email = string.Empty;
    string _password = string.Empty; 
    bool _showPassword = false;
    string _errorMessage = string.Empty;
    bool _isValid = false;
    private bool _registrationEnabled = false; // fail-safe: default hidden if the API call fails

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

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var flags = await FeatureFlags.GetAsync();
            _registrationEnabled = flags.RegistrationEnabled;
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to load feature flags: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            // Fallback to _registrationEnabled = false (safe default).
        }
    }
}
