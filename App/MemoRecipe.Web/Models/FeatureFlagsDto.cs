namespace MemoRecipe.Web.Models;

public class FeatureFlagsDto
{
    public bool ScanRecipeEnabled { get; set; }
    public bool RegistrationEnabled { get; set; } = false;
}
