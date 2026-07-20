using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;
using MemoRecipe.Web.Helpers;

namespace MemoRecipe.Web.Pages;

public partial class EditRecipe
{
    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    private RecipeDto? _recipe;
    private RecipeFormModel? _recipeForm;
    private string? _errorMessage;
    bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _recipe = await RecipeService.GetRecipeByIdAsync(Id);
            _recipeForm = RecipeMapper.MapRecipeDtoToFormModel(_recipe);
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du chargement de votre recette";
            Navigation.NavigateTo($"/recipes/{Id}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleModification(RecipeFormModel _recipeForm)
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var updtatedRecipe = RecipeMapper.MapToRecipeUpdateDto(_recipeForm);
            await RecipeService.UpdateRecipeAsync(Id, updtatedRecipe);
            Snackbar.Add("Recette sauvegardée !", Severity.Success, config =>
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            Navigation.NavigateTo($"/recipes/{Id}");
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
        Navigation.NavigateTo($"/recipes/{Id}");
    }

    private void RefreshUI() => StateHasChanged();
}
