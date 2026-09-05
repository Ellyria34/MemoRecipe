using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for the / (root) page — the authenticated dashboard.
/// Contains a "Bienvenue sur Memo Recipe" heading and the NavBar with the logout button.
/// </summary>
public class HomePage
{
    private readonly IPage _page;

    public HomePage(IPage page)
    {
        _page = page;
    }

    // ----- Navigation -----

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/");

    // ----- Locators -----

    // Main H1 on the dashboard — strong signal that login succeeded and Home rendered.
    public ILocator WelcomeHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Bienvenue sur Memo Recipe" });

    // NavBar logout button (MudIconButton with aria-label="Se déconnecter").
    // GetByRole(Button, Name=...) resolves the accessible name from aria-label for icon buttons.
    public ILocator LogoutButton => _page.GetByRole(AriaRole.Button, new() { Name = "Se déconnecter" });

    // NavBar profile button (for later scenarios if needed).
    public ILocator ProfileButton => _page.GetByRole(AriaRole.Button, new() { Name = "Mon profil" });

    // ----- Business actions -----

    public Task LogoutAsync() => LogoutButton.ClickAsync();
}
