namespace DriftDNS.Core.Models;

public class SyncState
{
    public Guid EndpointId { get; set; }
    public string? LastKnownPublicIp { get; set; }
    public string? LastAppliedIp { get; set; }
    public DateTime? LastCheckAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }

    public DnsEndpoint? Endpoint { get; set; }
}
