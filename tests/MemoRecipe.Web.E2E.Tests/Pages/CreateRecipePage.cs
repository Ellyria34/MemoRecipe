using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for /recipes/new. Uses the shared RecipeForm component + RecipeStickyActionBar.
/// </summary>
public class CreateRecipePage
{
    private readonly IPage _page;

    public CreateRecipePage(IPage page) { _page = page; }

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/recipes/new");

    // RecipeForm exposes Title as a MudTextField Required — accessible name is "Titre*"
    public ILocator TitleField => _page.GetByRole(AriaRole.Textbox, new() { Name = "Titre*", Exact = true });

    // Ingredients section
    public ILocator AddIngredientButton => _page.GetByRole(AriaRole.Button, new() { Name = "Ajouter un ingrédient" });

    // aria-label pattern: "Nom de l'ingrédient {displayNumber}" where displayNumber = index + 1
    public ILocator IngredientNameField(int displayNumber)
        => _page.GetByRole(AriaRole.Textbox, new() { Name = $"Nom de l'ingrédient {displayNumber}", Exact = true });

    // Steps section
    public ILocator AddStepButton => _page.GetByRole(AriaRole.Button, new() { Name = "Ajouter une étape" });

    // aria-label pattern: "Instruction de l'étape {stepNumber}"
    public ILocator StepInstructionField(int stepNumber)
        => _page.GetByRole(AriaRole.Textbox, new() { Name = $"Instruction de l'étape {stepNumber}", Exact = true });

    // RecipeStickyActionBar has TWO buttons (mobile + desktop, hidden via CSS breakpoint) — take the first.
    public ILocator SaveButton => _page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).First;

    /// <summary>Fills the minimum required for RecipeFormValidator to accept the recipe:
    /// title + 1 ingredient with a name + 1 step with an instruction, then clicks Save.
    /// See RecipeFormValidator.IsValid() for the exact business rules.
    /// The Expect(SaveButton).ToBeEnabledAsync() before ClickAsync is CRITICAL: FillAsync sets
    /// the value in one shot, but Blazor's async NotifyChange -> OnFormChanged -> StateHasChanged
    /// cycle needs a beat to propagate IsValid=true to the sticky bar. Without this wait,
    /// Playwright may click faster than Blazor can attach the onClick handler on the (visually)
    /// enabled button, and the click is silently ignored.</summary>
    public async Task CreateWithMinimalDataAsync(string title, string ingredientName, string stepInstruction)
    {
        await TitleField.FillAsync(title);

        await AddIngredientButton.ClickAsync();
        await IngredientNameField(1).FillAsync(ingredientName);

        await AddStepButton.ClickAsync();
        await StepInstructionField(1).FillAsync(stepInstruction);

        await Assertions.Expect(SaveButton).ToBeEnabledAsync();
        await SaveButton.ClickAsync();
    }
}