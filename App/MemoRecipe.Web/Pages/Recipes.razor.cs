using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;

namespace MemoRecipe.Web.Pages;

public partial class Recipes
{
    [Inject]
    private IRecipeService RecipeService {get; set;} = default!;

    [Inject]
    private NavigationManager Navigation {get; set;} = default!;

    List<RecipeDto>? _recipes = null;
    bool _isLoading = false;
    string? _errorMessage = null;


    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
           _recipes = await RecipeService.GetAllRecipesAsync(orderBy:"createdAt");
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du chargement de vos recettes";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleRedirection()
    {
        Navigation.NavigateTo($"/recipes/new");
    }
}