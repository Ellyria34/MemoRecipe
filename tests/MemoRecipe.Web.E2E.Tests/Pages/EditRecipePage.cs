using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for /recipes/{id:guid}/edit. Uses the same RecipeForm component as CreateRecipe.
/// </summary>
public class EditRecipePage
{
    private readonly IPage _page;

    public EditRecipePage(IPage page) { _page = page; }

    public ILocator TitleField => _page.GetByRole(AriaRole.Textbox, new() { Name = "Titre*", Exact = true });

    public ILocator SaveButton => _page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).First;

    /// <summary>Clears the title, fills the new value, waits for the Blazor state to propagate
    /// so IsValid is true, then clicks Save. Same reasoning as CreateRecipePage: without the
    /// ToBeEnabledAsync wait, Playwright can click faster than the Blazor render cycle.</summary>
    public async Task UpdateTitleAsync(string newTitle)
    {
        await TitleField.FillAsync(newTitle);   // FillAsync clears the field automatically before typing
        await Assertions.Expect(SaveButton).ToBeEnabledAsync();
        await SaveButton.ClickAsync();
    }
}