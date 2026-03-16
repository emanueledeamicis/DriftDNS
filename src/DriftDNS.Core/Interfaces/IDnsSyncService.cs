namespace DriftDNS.Core.Interfaces;

public interface IDnsSyncService
{
    Task RunSyncAsync(CancellationToken cancellationToken = default);
}
