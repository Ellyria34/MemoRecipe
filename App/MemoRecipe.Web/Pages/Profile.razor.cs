using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;


namespace MemoRecipe.Web.Pages;

public partial class Profile
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    string _password = string.Empty;
    private bool _showPassword;

    MudMessageBox _confirmDialog = default!;

    private string? _errorMessage;
    private string? ValidatePassword(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Le mot de passe est obligatoire";
        }
        return null;
    }
    private async Task OpenDeleteDialog()
    {
        _errorMessage = null;
        bool? result = await _confirmDialog.ShowAsync();
        if (result != true) return;

        var success = await AuthService.RequestAccountDeletionAsync(_password);
        if (success)
        {
            Snackbar.Add("Votre demande de suppression est prise en compte. Votre compte sera définitivement supprimé dans 30 jour !", Severity.Success, config =>
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            Navigation.NavigateTo("/login");
        }
        else
        {
            Snackbar.Add("Mot de passe incorrect.", Severity.Error, config =>
            {
                config.VisibleStateDuration = 5000;
                config.ShowCloseIcon = true;
            });
        }
    }
}