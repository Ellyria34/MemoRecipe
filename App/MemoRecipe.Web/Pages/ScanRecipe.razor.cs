using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using MemoRecipe.Web.Helpers;
using MemoRecipe.Web.Exceptions;

namespace MemoRecipe.Web.Pages;

public partial class ScanRecipe
{
    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IFeatureFlagsService FeatureFlags { get; set; } = default!;

    [Inject]
    private ILogger<ScanRecipe> Logger { get; set; } = default!;
    private bool _scanEnabled = false;

    private ExtractedRecipeDto? _extractedRecipe;
    private RecipeFormModel? _newRecipe;
    private string? _errorMessage;
    private IBrowserFile? _selectedFile;
    bool _isLoading = false;

    private void UploadFile(IBrowserFile file)
    {
        _selectedFile = file;
    }

    private async Task HandleGeneration()
    {
        if (_selectedFile == null)
        {
            return;
        }
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var stream = _selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            _extractedRecipe = await RecipeService.ScanImageAsync(stream, _selectedFile.ContentType, _selectedFile.Name);
            _newRecipe = RecipeMapper.MapExtractedRecipeDtoToFormModel(_extractedRecipe);
        }
        catch (AiRateLimitException ex)
        {
            _errorMessage = FormatRateLimitMessage(ex.RetryAfterSeconds);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatRateLimitMessage(int retryAfterSeconds)
    {
        if (retryAfterSeconds < 3600)
        {
            var minutes = Math.Ceiling(retryAfterSeconds / 60.0);
            return $"Limite quotidienne de scans atteinte. Réessayez dans {minutes} minute(s).";
        }
        var hours = Math.Ceiling(retryAfterSeconds / 3600.0);
        return $"Limite quotidienne de scans atteinte. Réessayez dans {hours} heure(s).";
    }


    private async Task HandleCreation(RecipeFormModel recipeFormModel)
    {
        var recipeCreateDto = RecipeMapper.MapToRecipeCreateDto(recipeFormModel);
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var newRecipe = await RecipeService.CreateRecipeAsync(recipeCreateDto);
            Snackbar.Add("Recette sauvegardée !", Severity.Success, config =>
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            Navigation.NavigateTo($"/recipes");
        }
        catch (RecipeLimitException ex)
        {
            _errorMessage = $"Vous avez atteint la limite de {ex.Limit} recettes. Supprimez-en pour en créer de nouvelles.";
        }

        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors de la sauvegarde de la recette";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void HandleCancel()
    {
        _newRecipe = null;
        _extractedRecipe = null;
        _selectedFile = null;
        _errorMessage = null;
    }

    private void RefreshUI() => StateHasChanged();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var flags = await FeatureFlags.GetAsync();
            _scanEnabled = flags.ScanRecipeEnabled;
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to load feature flags: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            // Fallback to _scanEnabled = false: safer to show "no AI" than to falsely claim AI is running.
        }
    }

    private void GoToManualCreation() => Navigation.NavigateTo("/recipes/new");
}