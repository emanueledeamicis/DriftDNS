using DriftDNS.Core.Models;

namespace DriftDNS.Core.Interfaces;

public interface IDnsProvider
{
    string Name { get; }

    Task ValidateCredentialsAsync(ProviderAccount account, CancellationToken cancellationToken = default);

    Task UpsertRecordAsync(
        ProviderAccount account,
        DnsEndpoint endpoint,
        string ipAddress,
        CancellationToken cancellationToken = default);
}
