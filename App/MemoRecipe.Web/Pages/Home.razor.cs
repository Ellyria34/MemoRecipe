using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;

namespace MemoRecipe.Web.Pages;
public partial class Home
{
    [Inject] 
    private IRecipeService RecipeService { get; set; }= default!;

    int _recipeCount = 0;
    List<RecipeDto>? _recipes = null;
    private string? _errorMessage;
    bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _recipes = await RecipeService.GetAllRecipesAsync(limit: 5, orderBy: "createdAt");
            _recipeCount = await RecipeService.GetRecipeCountAsync();
        }
        catch (Exception)
        {
            _errorMessage = "Un problème est survenu lors du chargement";
        }
        finally
        {
            _isLoading = false;
        }
    }
}