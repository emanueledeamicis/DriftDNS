namespace DriftDNS.Core.Models;

public class SyncLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EndpointId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OldIp { get; set; }
    public string? NewIp { get; set; }
    public string Action { get; set; } = SyncAction.NoChange;
    public string? Details { get; set; }

    public DnsEndpoint? Endpoint { get; set; }
}

public static class SyncAction
{
    public const string NoChange = "NoChange";
    public const string Updated = "Updated";
    public const string Failed = "Failed";
}
