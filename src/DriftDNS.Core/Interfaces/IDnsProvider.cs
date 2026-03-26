using DriftDNS.Core.Models;

namespace DriftDNS.Core.Interfaces;

public interface IDnsProvider
{
    string Name { get; }

    Task ValidateCredentialsAsync(ProviderAccount account, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListZonesAsync(ProviderAccount account, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListHostnamesAsync(ProviderAccount account, string zoneName, string recordType, CancellationToken cancellationToken = default);

    Task UpsertRecordAsync(
        ProviderAccount account,
        DnsEndpoint endpoint,
        string ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the record's TTL if it exists, or null if it was not found.</summary>
    Task<int?> VerifyRecordAsync(
        ProviderAccount account,
        DnsEndpoint endpoint,
        CancellationToken cancellationToken = default);
}
