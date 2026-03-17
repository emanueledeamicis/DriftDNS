namespace DriftDNS.Core.Models;

public class AppSettings
{
    public int Id { get; set; } = 1; // single-row config
    public int SyncIntervalMinutes { get; set; } = 5;
    public int LogRetentionHours { get; set; } = 24;
}
