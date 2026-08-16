namespace MemoRecipe.Application.Services.Monitoring;

public class AiCostAlertingOptions
{
    public const string SectionName = "AiCostAlerting";

    public Dictionary<string, AiCostProviderThresholds> PerProvider { get; set; } = new();
}

public class AiCostProviderThresholds
{
    public long DailyTokenThreshold { get; set; }
    public long WeeklyTokenThreshold { get; set; }
}