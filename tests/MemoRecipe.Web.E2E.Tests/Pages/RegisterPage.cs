using Microsoft.Playwright;

namespace MemoRecipe.Web.E2E.Tests.Pages;

/// <summary>
/// Page Object for the /Register page. Encapsulates locators and business actions.
/// </summary>
public class RegisterPage
{
    private readonly IPage _page;

    public RegisterPage(IPage page)
    {
        _page = page;
    }

    // ----- Navigation -----

    public Task GotoAsync() => _page.GotoAsync("http://localhost:8080/Register");

    // ----- Locators -----

    public ILocator EmailField => _page.GetByLabel("Email");
    public ILocator UserNameField => _page.GetByLabel("Nom d'utilisateur");

    // Use GetByRole(Textbox) to exclude MudBlazor "eye" buttons whose aria-labels
    // contain "mot de passe" (e.g. "Afficher le mot de passe" / "Afficher la confirmation du mot de passe").
    // The "*" suffix comes from MudBlazor's Required="true" appended to the accessible name.
    // Exact=true then disambiguates between "Mot de passe*" and "Confirmer le mot de passe*".
    public ILocator PasswordField => _page.GetByRole(AriaRole.Textbox, new() { Name = "Mot de passe*", Exact = true });
    public ILocator PasswordConfirmationField => _page.GetByRole(AriaRole.Textbox, new() { Name = "Confirmer le mot de passe*", Exact = true });

    public ILocator SubmitButton => _page.GetByRole(AriaRole.Button, new() { Name = "S'inscrire" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);
    public ILocator LoginLink => _page.GetByRole(AriaRole.Link, new() { Name = "Se connecter" });

    // ----- Business actions -----

    /// <summary>
    /// Fills all four register form fields (using the same value for password and confirmation)
    /// and clicks submit. Does NOT wait for redirect.
    /// </summary>
    public async Task FillAndSubmitAsync(string email, string userName, string password)
    {
        await EmailField.FillAsync(email);
        await UserNameField.FillAsync(userName);
        await PasswordField.FillAsync(password);
        await PasswordConfirmationField.FillAsync(password);
        await SubmitButton.ClickAsync();
    }
}