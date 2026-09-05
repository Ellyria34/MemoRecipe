using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for /recipes/scan. Uses MudFileUpload for JPEG upload + shared RecipeForm
/// for the preview edit + RecipeStickyActionBar for save.
/// Requires Features:ScanRecipeEnabled=true (default in appsettings.json).
/// In E2E, the IA Function returns a hardcoded "Cheesecake maison" via AI_PROVIDER=Fake.
/// </summary>
public class ScanRecipePage
{
    private readonly IPage _page;

    public ScanRecipePage(IPage page) { _page = page; }

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/recipes/scan");

    // Feature-enabled state: "Selectionner un fichier" button visible = ScanRecipeEnabled=true
    public ILocator SelectFileButton => _page.GetByRole(AriaRole.Button, new() { Name = "Selectionner un fichier" });

    // MudFileUpload wraps a hidden <input type="file"> — target it directly for SetInputFilesAsync
    public ILocator HiddenFileInput => _page.Locator("input[type='file']");

    // Note: the button label in Blazor has a typo ("Prévisualiser le résultats") — we match it verbatim
    public ILocator PreviewButton => _page.GetByRole(AriaRole.Button, new() { Name = "Prévisualiser le résultats" });

    // Preview section header shows after successful scan (RecipeForm rendered underneath)
    public ILocator PreviewHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Votre nouvelle recette" });

    // Save button from RecipeStickyActionBar (mobile + desktop = take First)
    public ILocator SaveButton => _page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).First;

    /// <summary>Uploads the JPEG file directly to the hidden input, waits for Blazor to render
    /// the "Prévisualiser" button (which appears after _selectedFile is set via the FilesChanged handler),
    /// then clicks it. Without the ToBeVisibleAsync wait, Playwright can click faster than Blazor's
    /// change-detection + re-render cycle, hitting a stale button reference.</summary>
    public async Task UploadAndPreviewAsync(string jpegPath)
    {
        await HiddenFileInput.SetInputFilesAsync(jpegPath);
        await Assertions.Expect(PreviewButton).ToBeVisibleAsync();
        await PreviewButton.ClickAsync();
    }

    /// <summary>Waits for the Save button to become enabled (Blazor state propagation) then clicks.</summary>
    public async Task SaveScannedRecipeAsync()
    {
        await Assertions.Expect(SaveButton).ToBeEnabledAsync();
        await SaveButton.ClickAsync();
    }
}