using DriftDNS.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace DriftDNS.App.Workers;

public class DnsSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DnsSyncWorker> _logger;
    private readonly int _intervalMinutes;

    public DnsSyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DnsSyncWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervalMinutes = configuration.GetValue<int>("SyncIntervalMinutes", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DnsSyncWorker started with {Interval} minute interval", _intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);

            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IDnsSyncService>();
            await syncService.RunSyncAsync(stoppingToken);
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
}
