using DriftDNS.Core.Interfaces;
using DriftDNS.Infrastructure.Data;

namespace DriftDNS.App.Workers;

public class DnsSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DnsSyncWorker> _logger;

    public DnsSyncWorker(IServiceScopeFactory scopeFactory, ILogger<DnsSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DnsSyncWorker started");

        // Force update on startup to reconcile Route53 with current state
        await RunSyncAsync(stoppingToken, forceUpdate: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = await GetIntervalAsync();
            _logger.LogInformation("Next sync in {Interval} minutes", interval);
            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken stoppingToken, bool forceUpdate = false)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IDnsSyncService>();
            await syncService.RunSyncAsync(stoppingToken, forceUpdate);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during DNS sync");
        }
    }

    private async Task<int> GetIntervalAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DriftDnsDbContext>();
            var settings = await db.AppSettings.FindAsync(1);
            return settings?.SyncIntervalMinutes ?? 5;
        }
        catch
        {
            return 5;
        }
    }
}
