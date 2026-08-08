using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;

namespace MemoRecipe.Web.Pages;

public partial class Recipes
{
    [Inject]
    private IBrowserViewportService ViewportService { get; set; } = default!;

    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private int _currentPage = 1;
    private int _pageSize = 5;
    PagedResult<RecipeDto>? _recipes = null;
    bool _isLoading = false;
    string? _errorMessage = null;

    protected override async Task OnInitializedAsync()
    {
        var breakpoint = await ViewportService.GetCurrentBreakpointAsync();
        _pageSize = breakpoint >= Breakpoint.Md ? 10 : 5;

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

    private async Task OnPageChanged(int newPage)
    {
        _currentPage = newPage;
        await LoadRecipesAsync();
    }

    private async Task HandleRedirection()
    {
        Navigation.NavigateTo($"/recipes/new");
    }

}