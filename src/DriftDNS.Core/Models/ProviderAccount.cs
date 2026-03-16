namespace DriftDNS.Core.Models;

public class ProviderAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string EncryptedCredentials { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DnsEndpoint> DnsEndpoints { get; set; } = new List<DnsEndpoint>();
}
