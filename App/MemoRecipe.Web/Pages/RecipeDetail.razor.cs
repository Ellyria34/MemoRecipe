using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;


namespace MemoRecipe.Web.Pages;
public partial class RecipeDetail
{
    [Inject] 
    private IRecipeService RecipeService {get; set;} = null!;

    [Inject] 
    private NavigationManager Navigation {get; set;} = null!;

    [Inject] 
    private IDialogService DialogService {get; set;} = null!;

    [Inject] 
    private ISnackbar Snackbar {get; set;} = null!;

    [Parameter]
    public Guid Id { get; set; }

    private RecipeDto? _recipe;
    bool _isLoading = false;
    string? _errorMessage = "";

    MudMessageBox _confirmDialog = null!;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _recipe = await RecipeService.GetRecipeByIdAsync(Id);
            _recipe.Steps = _recipe.Steps.OrderBy(s => s.Order).ToList();
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du chargement de votre recette";
            //attendre un peu
            Navigation.NavigateTo($"/recipes");
        }
        finally
        {
            _isLoading = false;
        }
    }
    private async Task HandleRedirection()
    {
        Navigation.NavigateTo($"/recipes");
    }

    private async Task Delete()
    {
        bool? result = await _confirmDialog.ShowAsync();
        if (result != true) return;

        await RecipeService.DeleteRecipe(Id);
        Snackbar.Add("Recette supprimée !", Severity.Success, config =>
        {
            config.VisibleStateDuration = 1500;
            config.ShowCloseIcon = false;
        });
        Navigation.NavigateTo("/recipes");
    }
}