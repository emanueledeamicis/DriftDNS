namespace DriftDNS.Core.Interfaces;

public interface IDnsSyncService
{
    Task RunSyncAsync(CancellationToken cancellationToken = default, bool forceUpdate = false);
    Task SyncEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
