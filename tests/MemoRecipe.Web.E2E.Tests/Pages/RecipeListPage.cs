using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for /recipes (list of user's recipes).
/// </summary>
public class RecipeListPage
{
    private readonly IPage _page;

    public RecipeListPage(IPage page) { _page = page; }

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/recipes");

    // Main H1 "Mes recettes" — signal the page rendered.
    public ILocator PageHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Mes recettes" });

    /// <summary>Locator for the "eye" icon link that opens a specific recipe (aria-label="Voir la recette {title}").
    /// MudIconButton with Href renders as an HTML anchor (role="link"), not a button.</summary>
    public ILocator RecipeViewLink(string title)
        => _page.GetByRole(AriaRole.Link, new() { Name = $"Voir la recette {title}" });

    /// <summary>Click the view link on a specific recipe card to navigate to its detail page.</summary>
    public Task OpenRecipeAsync(string title) => RecipeViewLink(title).ClickAsync();
}