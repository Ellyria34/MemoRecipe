using MemoRecipe.Web.E2E.Tests.Helpers;
using MemoRecipe.Web.E2E.Tests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class RecipeCreateTests : PageTest
{
    [Fact]
    public async Task Recipe_CreateEditVerify_WorksEndToEnd()
    {
        // Arrange: unique user + recipe titles per test run for isolation
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"e2e-create-{timestamp}@example.com";
        var userName = $"e2e-create-{timestamp}";
        var password = "E2eCreatePassword!2026";
        var recipeTitle = $"E2E Recipe {timestamp}";
        var updatedTitle = $"{recipeTitle} EDITED";

        // Seed the user via HTTP (bypasses the UI register flow, faster and more robust)
        await TestUserHelper.CreateUserViaHttpAsync(email, userName, password);

        var loginPage = new LoginPage(Page);
        var homePage = new HomePage(Page);
        var createRecipePage = new CreateRecipePage(Page);
        var listPage = new RecipeListPage(Page);
        var detailPage = new RecipeDetailPage(Page);
        var editPage = new EditRecipePage(Page);

        // ----- Step 1: Login via UI + WAIT for the auth session to be established -----
        // Without this assertion, the next GotoAsync races the login redirect and
        // navigates to /recipes/new before the auth cookie is set → server redirects
        // to /login (Authorize attribute) → the Titre field never appears → timeout.
        await loginPage.GotoAsync();
        await loginPage.FillAndSubmitAsync(email, password);
        await Expect(homePage.WelcomeHeading).ToBeVisibleAsync();

        // ----- Step 2: Navigate to create-recipe form and save with minimal valid data -----
        // RecipeFormValidator requires: Title (3-200 chars) + >= 1 ingredient with a name + >= 1 step with an instruction.
        await createRecipePage.GotoAsync();
        await createRecipePage.CreateWithMinimalDataAsync(recipeTitle, "Farine", "Mélanger les ingrédients");

        // CreateRecipe.razor.cs auto-redirects to /recipes on save success.
        // Wait for the redirect explicitly — DO NOT navigate manually (would race the API POST).
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/recipes$"));

        // ----- Step 3: Assert the new recipe is visible in the list -----
        await Expect(listPage.PageHeading).ToBeVisibleAsync();
        await Expect(listPage.RecipeViewLink(recipeTitle)).ToBeVisibleAsync();

        // ----- Step 4: Open the recipe detail and assert the title renders as the H1 -----
        await listPage.OpenRecipeAsync(recipeTitle);
        await Expect(detailPage.TitleHeading(recipeTitle)).ToBeVisibleAsync();

        // ----- Step 5: Click "Modifier", wait for edit form, change the title, save -----
        await detailPage.ClickEditAsync();
        await Expect(editPage.TitleField).ToBeVisibleAsync();  // ensure edit page loaded
        await editPage.UpdateTitleAsync(updatedTitle);

        // ----- Step 6: EditRecipe.razor.cs redirects to /recipes/{id} on save success -----
        // The detail page reloads with the updated title as H1 — assert it directly.
        await Expect(detailPage.TitleHeading(updatedTitle)).ToBeVisibleAsync();
    }
}