using System.Text.RegularExpressions;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class SmokeTest : PageTest
{
    [Fact]
    public async Task Playwright_CanOpenExamplePage_TitleContainsExample()
    {
        // Arrange & Act : navigate to a public, stable page
        await Page.GotoAsync("https://example.com");

        // Assert : title contains "Example Domain"
        await Expect(Page).ToHaveTitleAsync(new Regex("Example"));
    }
}