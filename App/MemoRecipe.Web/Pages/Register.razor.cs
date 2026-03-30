using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;

namespace MemoRecipe.Web.Pages;

public partial class Register
{
    [Inject]
    private IAuthService AuthService {get; set;} = default!;

    [Inject]
    private NavigationManager Navigation {get; set;} = default!;

    string _email = string.Empty;
    string _userName = string.Empty;
    string _password = string.Empty;
    string _passwordConfirmation = string.Empty;
    bool _showPassword = false;
    string _errorMessage = string.Empty;
    bool _isValid = false;

    private string? ValidateEmail(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "L'email est obligatoire";
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return "L'adresse mail n'est pas valide.";
        return null;
    }

    private string? ValidateUserName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "Le nom d'utilisateur est obligatoire";
        if (value.Length < 3)
            return "Le nom d'utilisateur doit faire au moins 3 caractères";
        if (value.Length > 50)
            return "Le nom d'utilisateur ne doit pas dépasser 50 caractères";
        return null;
    }

    private string? ValidatePassword(string value)
    {
        if (string.IsNullOrEmpty(value)) 
            return "Le mot de passe est obligatoire";
        
        var manque = new List<string>();
        if (value.Length < 8) manque.Add("8 caractères minimum");
        if (value.Length > 100) manque.Add("100 caractères maximum");
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"[A-Z]")) manque.Add("une majuscule");
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"[a-z]")) manque.Add("une minuscule");
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"[0-9]")) manque.Add("un chiffre");
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"[^a-zA-Z0-9]")) manque.Add("un caractère spécial");
        
        return manque.Any() ? "Le mot de passe doit contenir : " + string.Join(", ", manque) : null;
    }

    private string? ValidatePasswordConfirmation(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "La confirmation du mot de passe est obligatoire";
        if (_password != _passwordConfirmation)
            return "Les deux mots de passe ne sont pas identiques";
        return null;
    }

    private async Task HandleRegister()
    {
        _errorMessage = string.Empty;
        bool success = false;
        if (!_isValid) return;

        try
        {
            var result = await AuthService.RegisterAsync(_email, _userName, _password);
            if(!result)
            {
                _errorMessage = "L'enregistrement n'a pas fonctionné, veuillez retenter";
                return;
            }
            success = true;
        }
        catch (Exception)
        {
            _errorMessage = "Une erreur est survenue lors de l'inscription";
        }
        if(success)
        {
            Navigation.NavigateTo("/login");
        }
    }
}