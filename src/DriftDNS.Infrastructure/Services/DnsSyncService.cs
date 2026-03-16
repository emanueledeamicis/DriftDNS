using DriftDNS.Core.Interfaces;
using DriftDNS.Core.Models;
using DriftDNS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DriftDNS.Infrastructure.Services;

public class DnsSyncService : IDnsSyncService
{
    private readonly DriftDnsDbContext _db;
    private readonly IPublicIpResolver _ipResolver;
    private readonly IEnumerable<IDnsProvider> _providers;
    private readonly ILogger<DnsSyncService> _logger;

    public DnsSyncService(
        DriftDnsDbContext db,
        IPublicIpResolver ipResolver,
        IEnumerable<IDnsProvider> providers,
        ILogger<DnsSyncService> logger)
    {
        _db = db;
        _ipResolver = ipResolver;
        _providers = providers;
        _logger = logger;
    }

    public async Task RunSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DNS sync started");

        var currentIp = await _ipResolver.ResolveAsync(cancellationToken);
        if (currentIp is null)
        {
            _logger.LogError("DNS sync aborted: could not resolve public IP");
            return;
        }

        var endpoints = await _db.DnsEndpoints
            .Where(e => e.Enabled)
            .Include(e => e.ProviderAccount)
            .Include(e => e.SyncState)
            .ToListAsync(cancellationToken);

        foreach (var endpoint in endpoints)
        {
            await SyncEndpointAsync(endpoint, currentIp, cancellationToken);
        }

        _logger.LogInformation("DNS sync completed");
    }

    private async Task SyncEndpointAsync(DnsEndpoint endpoint, string currentIp, CancellationToken cancellationToken)
    {
        var state = endpoint.SyncState ?? new SyncState { EndpointId = endpoint.Id };

        state.LastKnownPublicIp = currentIp;
        state.LastCheckAt = DateTime.UtcNow;

        if (state.LastAppliedIp == currentIp)
        {
            _logger.LogDebug("No change for {Hostname}", endpoint.Hostname);
            await WriteLogAsync(endpoint.Id, SyncAction.NoChange, state.LastAppliedIp, currentIp, "IP unchanged");
            await UpsertStateAsync(state);
            return;
        }

        var provider = _providers.FirstOrDefault(p =>
            p.Name.Equals(endpoint.ProviderAccount?.ProviderType, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            var msg = $"No provider found for type '{endpoint.ProviderAccount?.ProviderType}'";
            _logger.LogError(msg);
            state.LastError = msg;
            await WriteLogAsync(endpoint.Id, SyncAction.Failed, state.LastAppliedIp, currentIp, msg);
            await UpsertStateAsync(state);
            return;
        }

        try
        {
            await provider.UpsertRecordAsync(endpoint.ProviderAccount!, endpoint, currentIp, cancellationToken);

            var oldIp = state.LastAppliedIp;
            state.LastAppliedIp = currentIp;
            state.LastSuccessAt = DateTime.UtcNow;
            state.LastError = null;

            _logger.LogInformation("Updated {Hostname}: {OldIp} → {NewIp}", endpoint.Hostname, oldIp, currentIp);
            await WriteLogAsync(endpoint.Id, SyncAction.Updated, oldIp, currentIp, "DNS record updated successfully");
        }
        catch (Exception ex)
        {
            state.LastError = ex.Message;
            _logger.LogError(ex, "Failed to update {Hostname}", endpoint.Hostname);
            await WriteLogAsync(endpoint.Id, SyncAction.Failed, state.LastAppliedIp, currentIp, ex.Message);
        }

        await UpsertStateAsync(state);
    }

    private async Task UpsertStateAsync(SyncState state)
    {
        var existing = await _db.SyncStates.FindAsync(state.EndpointId);
        if (existing is null)
            _db.SyncStates.Add(state);
        else
        {
            existing.LastKnownPublicIp = state.LastKnownPublicIp;
            existing.LastAppliedIp = state.LastAppliedIp;
            existing.LastCheckAt = state.LastCheckAt;
            existing.LastSuccessAt = state.LastSuccessAt;
            existing.LastError = state.LastError;
        }
        await _db.SaveChangesAsync();
    }

    private async Task WriteLogAsync(Guid endpointId, string action, string? oldIp, string? newIp, string? details)
    {
        _db.SyncLogs.Add(new SyncLog
        {
            EndpointId = endpointId,
            Action = action,
            OldIp = oldIp,
            NewIp = newIp,
            Details = details,
            Timestamp = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }
}
