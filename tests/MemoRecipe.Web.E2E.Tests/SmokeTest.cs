using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class SmokeTest : PageTest
{
    [Fact]
    public async Task Home_LoadsSuccessfully_TitleContainsMemoRecipe()
    {
        // Arrange & Act: navigate to the local E2E stack homepage
        await Page.GotoAsync("http://localhost:8080");

        // Assert: the page title contains "MemoRecipe" (regardless of the tagline suffix)
        await Expect(Page).ToHaveTitleAsync(new Regex("MemoRecipe"));
    }
}
