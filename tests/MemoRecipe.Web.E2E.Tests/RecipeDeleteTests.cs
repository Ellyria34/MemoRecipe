using System.Text.RegularExpressions;
using MemoRecipe.Web.E2E.Tests.Helpers;
using MemoRecipe.Web.E2E.Tests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class RecipeDeleteTests : PageTest
{
    [Fact]
    public async Task Recipe_DeleteWithConfirmation_RemovesFromList()
    {
        // Arrange: unique user + recipe title for isolation across test runs
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"e2e-delete-{timestamp}@example.com";
        var userName = $"e2e-delete-{timestamp}";
        var password = "E2eDeletePassword!2026";
        var recipeTitle = $"E2E Delete Recipe {timestamp}";

        await TestUserHelper.CreateUserViaHttpAsync(email, userName, password);

        var loginPage = new LoginPage(Page);
        var homePage = new HomePage(Page);
        var createRecipePage = new CreateRecipePage(Page);
        var listPage = new RecipeListPage(Page);
        var detailPage = new RecipeDetailPage(Page);

        // ----- Step 1: Login + wait for dashboard -----
        await loginPage.GotoAsync();
        await loginPage.FillAndSubmitAsync(email, password);
        await Expect(homePage.WelcomeHeading).ToBeVisibleAsync();

        // ----- Step 2: Create a recipe (setup for the delete action) -----
        await createRecipePage.GotoAsync();
        await createRecipePage.CreateWithMinimalDataAsync(recipeTitle, "Sel", "Cuire au four 30 minutes");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes$"), new() { Timeout = 15000 });
        await Expect(listPage.RecipeViewLink(recipeTitle)).ToBeVisibleAsync();

        // ----- Step 3: Open the recipe detail -----
        await listPage.OpenRecipeAsync(recipeTitle);
        await Expect(detailPage.TitleHeading(recipeTitle)).ToBeVisibleAsync();

        // ----- Step 4: Click Supprimer + confirm "Oui, supprimer" in MudMessageBox -----
        await detailPage.DeleteWithConfirmationAsync();

        // ----- Step 5: Assert redirect + recipe is NO LONGER visible in the list -----
        // Not.ToBeVisibleAsync = negative assertion — verifies element is absent
        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes$"), new() { Timeout = 15000 });
        await Expect(listPage.RecipeViewLink(recipeTitle)).Not.ToBeVisibleAsync();
    }
}