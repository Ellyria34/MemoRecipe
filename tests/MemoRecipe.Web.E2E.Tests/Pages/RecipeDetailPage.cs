using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for /recipes/{id:guid} (recipe detail view).
/// </summary>
public class RecipeDetailPage
{
    private readonly IPage _page;

    public RecipeDetailPage(IPage page) { _page = page; }

    /// <summary>Locator matching the H1 heading which contains the recipe title.</summary>
    public ILocator TitleHeading(string title)
        => _page.GetByRole(AriaRole.Heading, new() { Name = title, Level = 1 });

    public ILocator EditButton => _page.GetByRole(AriaRole.Link, new() { Name = "Modifier" });
    public ILocator DeleteButton => _page.GetByRole(AriaRole.Button, new() { Name = "Supprimer" });

    // MudMessageBox confirmation for deletion
    public ILocator ConfirmDeleteYesButton => _page.GetByRole(AriaRole.Button, new() { Name = "Oui, supprimer" });

    public Task ClickEditAsync() => EditButton.ClickAsync();

    /// <summary>Clicks Delete then confirms in the MudMessageBox dialog.</summary>
    public async Task DeleteWithConfirmationAsync()
    {
        await DeleteButton.ClickAsync();
        await ConfirmDeleteYesButton.ClickAsync();
    }
}