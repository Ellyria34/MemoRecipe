using MudBlazor;

namespace MemoRecipe.Web.Layout;

public partial class AuthLayout
{
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#8FBF9F",
            PrimaryContrastText = "#2D3436",
            Secondary = "#6A9C7E",
            Background = "#FAF9F6",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#2D3436",
            TextPrimary = "#2D3436",
            TextSecondary = "#4A4E52",
            ActionDefault = "#2D3436",
            DrawerBackground = "#FAF9F6",
            DrawerText = "#2D3436",
            Error = "#D32F2F",
            Success = "#6A9C7E"
        }
    };
}