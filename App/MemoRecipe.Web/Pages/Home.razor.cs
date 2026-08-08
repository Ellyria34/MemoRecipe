using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;

namespace MemoRecipe.Web.Pages;

public partial class Home
{
    [Inject]
    private IBrowserViewportService ViewportService { get; set; } = default!;

    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    int _recipeCount = 0;
    private int _currentPage = 1;
    private int _pageSize = 3;
    PagedResult<RecipeDto>? _recipes = null;
    private string? _errorMessage;
    bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        var breakpoint = await ViewportService.GetCurrentBreakpointAsync();
        _pageSize = breakpoint >= Breakpoint.Md ? 5 : 3;
        _recipeCount = await RecipeService.GetRecipeCountAsync();
        await LoadRecipesAsync();
    }

    private async Task LoadRecipesAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _recipes = await RecipeService.GetAllRecipesAsync(
                page: _currentPage,
                pageSize: _pageSize,
                orderBy: "createdAt");

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
}