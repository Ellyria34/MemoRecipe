using MemoRecipe.Web.E2E.Tests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MemoRecipe.Web.E2E.Tests;

public class AuthTests : PageTest
{
    [Fact]
    public async Task Auth_RegisterLoginLogoutRelogin_AllStepsSucceed()
    {
        // Arrange: unique email + username per test run to avoid collisions across runs
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"e2e-auth-{timestamp}@example.com";
        var userName = $"e2e-auth-{timestamp}";
        var password = "E2eAuthPassword!2026";  // meets standard validation (>= 8 chars, mixed case, digit, special)

        var registerPage = new RegisterPage(Page);
        var loginPage = new LoginPage(Page);
        var homePage = new HomePage(Page);

        // ----- Step 1: Register a new user -----
        await registerPage.GotoAsync();
        await registerPage.FillAndSubmitAsync(email, userName, password);

        // Assert redirect to /login (Register.razor.cs Navigation.NavigateTo("/login") on success)
        // This surfaces register failures immediately instead of failing later during login.
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/login"));

        // ----- Step 2: Login with the newly created credentials -----
        await loginPage.GotoAsync();
        await loginPage.FillAndSubmitAsync(email, password);

        // ----- Step 3: Assert dashboard rendered (login succeeded) -----
        await Expect(homePage.WelcomeHeading).ToBeVisibleAsync();

        // ----- Step 4: Logout via the NavBar icon button -----
        await homePage.LogoutAsync();

        // ----- Step 5: Re-login with the same credentials -----
        await loginPage.GotoAsync();
        await loginPage.FillAndSubmitAsync(email, password);

        // ----- Step 6: Assert dashboard rendered again (re-login succeeded) -----
        await Expect(homePage.WelcomeHeading).ToBeVisibleAsync();
    }
}
