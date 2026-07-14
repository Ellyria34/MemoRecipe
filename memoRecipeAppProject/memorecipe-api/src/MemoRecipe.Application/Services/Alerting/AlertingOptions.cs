namespace MemoRecipe.Application.Services.Alerting;

public class AlertingOptions
{
    public const string SectionName = "Alerting";
    public int MassPurgeCritical {get; set;} = 10;
    public AlertingOptions? Value { get; internal set; }
}