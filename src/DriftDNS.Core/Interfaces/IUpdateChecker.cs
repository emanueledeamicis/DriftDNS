namespace DriftDNS.Core.Interfaces;

public interface IUpdateChecker
{
    Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default);
}
