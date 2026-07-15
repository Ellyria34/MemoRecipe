namespace MemoRecipe.Application.Services.Alerting;

public class AlertingOptions
{
    public const string SectionName = "Alerting";
    public int MassPurgeCritical { get; set; } = 10;
    public int LoginFailStormCritical { get; set; } = 50;
    public TimeSpan LoginFailStormWindow { get; set; } = TimeSpan.FromMinutes(5);
    public int ServerErrorSpikeCritical { get; set; } = 20;
    public TimeSpan ServerErrorSpikeWindow { get; set; } = TimeSpan.FromMinutes(5);
    public string BackupPath {get; set;} = "/backups";
    public TimeSpan BackupStaleAfter {get; set;} = TimeSpan.FromHours(26);
}