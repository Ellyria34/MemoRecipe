namespace MemoRecipe.Application.Services.AISecurity;

public class AiRateLimitOptions
{
    public const string SectionName = "AiRateLimiting";

    public int PerUserPerHour { get; set; } = 20;
    public int PerUserPerDay { get; set; } = 5;
    public int PerIpPerHour { get; set; } = 30;
    public int GlobalPerMinute { get; set; } = 20;
}
