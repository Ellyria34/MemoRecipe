namespace MemoRecipe.Application.Configuration;

public class AccountPurgeOptions
{
    public const string SectionName = "AccountPurge";

    public bool Enabled { get; set; } = true;

    public int IntervalHours { get; set; } = 24;

    public int PurgeAfterDays { get; set; } = 30;
}
