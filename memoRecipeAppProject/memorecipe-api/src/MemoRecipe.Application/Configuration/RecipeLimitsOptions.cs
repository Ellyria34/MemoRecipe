namespace MemoRecipe.Application.Configuration;

public class RecipeLimitsOptions
{
    public const string SectionName = "RecipeLimits";
    public int MaxPerUser { get; set; } = 200;
}