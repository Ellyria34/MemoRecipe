using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;
using MemoRecipe.Web.Services;
using MudBlazor;


namespace MemoRecipe.Web.Pages;

public partial class RecipeDetail
{
    [Inject]
    private IRecipeService RecipeService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    private RecipeDto? _recipe;

    MudMessageBox _confirmDialog = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _recipe = await RecipeService.GetRecipeByIdAsync(Id);
            _recipe.Steps = _recipe.Steps.OrderBy(s => s.Order).ToList();
        }
        catch (Exception)
        {
            Navigation.NavigateTo($"/recipes");
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

        try
        {
            await RecipeService.DeleteRecipe(Id);
            Snackbar.Add("Recette supprimée !", Severity.Success, config =>
            {
                config.VisibleStateDuration = 1500;
                config.ShowCloseIcon = false;
            });
            Navigation.NavigateTo("/recipes");
        }
        catch
        {
            Snackbar.Add("Échec de la suppression. Veuillez réessayer.", Severity.Error, config =>
            {
                config.VisibleStateDuration = 5000;
                config.ShowCloseIcon = true;
            });
        }
    }

    private static string GetDifficultyLabel(DifficultyLevel? difficulty) => difficulty switch
    {
        DifficultyLevel.Easy => "Facile",
        DifficultyLevel.Medium => "Moyen",
        DifficultyLevel.Hard => "Difficile",
        _ => ""
    };

    private static string FormatIngredient(IngredientDto ing)
    {
        if (!string.IsNullOrWhiteSpace(ing.Unit) && ing.Quantity.HasValue)
        {
            var connector = StartsWithVowel(ing.Name) ? "d'" : "de ";
            return $"{ing.Quantity.Value.ToString("0.##")} {ing.Unit} {connector}{ing.Name}";
        }

        if (ing.Quantity.HasValue && ing.Quantity.Value > 0)
        {
            var name = ing.Quantity.Value > 1 ? Pluralize(ing.Name) : ing.Name;
            return $"{ing.Quantity.Value.ToString("0.##")} {name}";
        }

        return ing.Name;
    }

    private static bool StartsWithVowel(string s) =>
        !string.IsNullOrEmpty(s) && "aàâeéèêëiïîoôuùûhAÀÂEÉÈÊËIÏÎOÔUÙÛH".Contains(s[0]);

    private static string Pluralize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var last = char.ToLowerInvariant(name[^1]);
        if (last == 's' || last == 'x' || last == 'z') return name;
        return name + "s";
    }


}