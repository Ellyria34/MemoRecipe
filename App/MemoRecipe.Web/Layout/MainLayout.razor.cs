using MudBlazor;

namespace MemoRecipe.Web.Layout;

public partial class MainLayout
{
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#8FBF9F",
            Secondary = "#6A9C7E",
            Background = "#FAF9F6",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#2D3436",
            TextPrimary = "#2D3436",
            TextSecondary = "#6C757D",
            ActionDefault = "#2D3436",
            DrawerBackground = "#FAF9F6",
            DrawerText = "#2D3436",
            Error = "#D32F2F",
            Success = "#6A9C7E"
        }
    };
}
