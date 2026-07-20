namespace MemoRecipe.Application.Configuration;

public class FeatureFlagsOptions
{
    public const string SectionName = "Features";
    public bool ScanRecipeEnabled {get; set;} = false;
}