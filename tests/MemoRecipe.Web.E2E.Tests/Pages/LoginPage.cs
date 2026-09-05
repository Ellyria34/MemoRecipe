using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for the /login page. Encapsulates locators and business actions.
/// </summary>
public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    // ----- Navigation -----

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/login");

    // ----- Locators -----

    public ILocator EmailField => _page.GetByLabel("Email");
    // Use GetByRole(Textbox) to exclude the MudBlazor "eye" button whose aria-label also contains "Mot de passe".
    // The "*" comes from MudBlazor's Required="true" that appends it to the accessible name.
    public ILocator PasswordField => _page.GetByRole(AriaRole.Textbox, new() { Name = "Mot de passe*", Exact = true });
    public ILocator SubmitButton => _page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);
    public ILocator RegisterLink => _page.GetByRole(AriaRole.Link, new() { Name = "S'inscrire" });

    // ----- Business actions -----

    /// <summary>
    /// Fills the login form and clicks submit. Does NOT wait for redirect.
    /// </summary>
    public async Task FillAndSubmitAsync(string email, string password)
    {
        await EmailField.FillAsync(email);
        await PasswordField.FillAsync(password);
        await SubmitButton.ClickAsync();
    }
}
