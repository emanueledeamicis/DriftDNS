namespace DriftDNS.Core.Models;

public class DnsEndpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Hostname { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string RecordType { get; set; } = "A";
    public Guid ProviderAccountId { get; set; }
    public int TTL { get; set; } = 300;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProviderAccount? ProviderAccount { get; set; }
    public SyncState? SyncState { get; set; }
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
}
