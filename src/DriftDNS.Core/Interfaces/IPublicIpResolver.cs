namespace DriftDNS.Core.Interfaces;

public interface IPublicIpResolver
{
    Task<string?> ResolveAsync(CancellationToken cancellationToken = default);
}
