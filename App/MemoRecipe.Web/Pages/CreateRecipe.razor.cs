using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;
using MemoRecipe.Web.Helpers;

namespace MemoRecipe.Web.Pages;

public partial class CreateRecipe
{
    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;
    

    private RecipeFormModel _newRecipe = new RecipeFormModel
    {
        Title = "",
        Servings = 1,
        IsPublic = false,
        Ingredients = new List<IngredientFormModel>(),

        Steps = new List<StepFormModel>(),
    };

    private MudMessageBox _confirmDialog = default!;
    private string? _errorMessage;
    bool _isLoading = false;

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
            //TODO: When recipe details page was done redirecte to "/recipes/{newRecipe.Id}""
            Navigation.NavigateTo($"/recipes");
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

    private async Task HandleCancel()
    {
        bool? result = await _confirmDialog.ShowAsync();
        if (result != true) return;
        Navigation.NavigateTo($"/recipes/");
    }
}