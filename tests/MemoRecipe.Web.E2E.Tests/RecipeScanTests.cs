using System.Text.RegularExpressions;
using MemoRecipe.Web.E2E.Tests.Helpers;
using MemoRecipe.Web.E2E.Tests.Pages;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class RecipeScanTests : PageTest
{
    [Fact]
    public async Task Recipe_ScanUploadAndSave_UsesFakeIaAndPersists()
    {
        // Arrange: unique user + path to the test JPEG copied to bin at build time
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"e2e-scan-{timestamp}@example.com";
        var userName = $"e2e-scan-{timestamp}";
        var password = "E2eScanPassword!2026";

        // FakeChatCompletionClient always returns "Cheesecake maison" as the title, ignoring the image content.
        var expectedTitleFromFake = "Cheesecake maison";
        var testJpegPath = Path.Combine(AppContext.BaseDirectory, "Assets", "test-cheesecake.jpg");

        await TestUserHelper.CreateUserViaHttpAsync(email, userName, password);

        var loginPage = new LoginPage(Page);
        var homePage = new HomePage(Page);
        var scanPage = new ScanRecipePage(Page);
        var listPage = new RecipeListPage(Page);

        // ----- Step 1: Login + wait for dashboard -----
        await loginPage.GotoAsync();
        await loginPage.FillAndSubmitAsync(email, password);
        await Expect(homePage.WelcomeHeading).ToBeVisibleAsync();

        // ----- Step 2: Navigate to scan page + assert feature enabled -----
        // Extended timeout: ScanRecipe.razor.cs OnInitializedAsync makes an HTTP call to
        // /api/config/features (via FeatureFlagsService) before rendering the enabled state.
        // The page shows a MudProgressCircular during this call, and the button only appears
        // after the async response + Blazor re-render. Default 5s can be tight at cold start.
        await scanPage.GotoAsync();
        await Expect(scanPage.SelectFileButton).ToBeVisibleAsync(new() { Timeout = 15000 });

        // ----- Step 3: Upload the JPEG + trigger Preview -----
        await scanPage.UploadAndPreviewAsync(testJpegPath);

        // ----- Step 4: Wait for preview to render (Fake IA parse ~1-3s, allow 30s for cold start) -----
        await Expect(scanPage.PreviewHeading).ToBeVisibleAsync(new() { Timeout = 30000 });

        // ----- Step 5: Save the scanned recipe (title pre-filled with "Cheesecake maison") -----
        await scanPage.SaveScannedRecipeAsync();

        // ----- Step 6: Assert redirect to /recipes + recipe visible in list -----
        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes$"));
        await Expect(listPage.RecipeViewLink(expectedTitleFromFake)).ToBeVisibleAsync();
    }
}
