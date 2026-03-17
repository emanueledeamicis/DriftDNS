using DriftDNS.Core.Interfaces;
using DriftDNS.Core.Models;
using DriftDNS.Infrastructure.Data;
using DriftDNS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DriftDNS.Tests;

[TestFixture]
public class DnsSyncServiceTests : IDisposable
{
    private SqliteConnection _connection = null!;
    private DriftDnsDbContext _db = null!;
    private Mock<IPublicIpResolver> _ipResolver = null!;
    private Mock<IDnsProvider> _provider = null!;
    private DnsSyncService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DriftDnsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DriftDnsDbContext(options);
        _db.Database.EnsureCreated();

        _ipResolver = new Mock<IPublicIpResolver>();
        _provider = new Mock<IDnsProvider>();
        _provider.Setup(p => p.Name).Returns("Route53");

        _service = new DnsSyncService(_db, _ipResolver.Object, [_provider.Object], Mock.Of<ILogger<DnsSyncService>>());
    }

    // --- SyncEndpointAsync(Guid) ---

    [Test]
    public async Task SyncEndpointAsync_CreatesUpdatedLog_WhenEndpointExists()
    {
        var endpoint = await SeedEndpointAsync();
        _ipResolver.Setup(r => r.ResolveAsync(default)).ReturnsAsync("1.2.3.4");
        _provider.Setup(p => p.UpsertRecordAsync(It.IsAny<ProviderAccount>(), It.IsAny<DnsEndpoint>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SyncEndpointAsync(endpoint.Id);

        var log = await _db.SyncLogs.SingleAsync(l => l.EndpointId == endpoint.Id);
        log.Action.Should().Be(SyncAction.Updated);
        log.NewIp.Should().Be("1.2.3.4");
    }

    [Test]
    public async Task SyncEndpointAsync_OnlySyncsSpecifiedEndpoint()
    {
        var provider = await SeedProviderAsync();
        var ep1 = await SeedEndpointAsync(provider, "a.example.com");
        await SeedEndpointAsync(provider, "b.example.com");

        _ipResolver.Setup(r => r.ResolveAsync(default)).ReturnsAsync("1.2.3.4");
        _provider.Setup(p => p.UpsertRecordAsync(It.IsAny<ProviderAccount>(), It.IsAny<DnsEndpoint>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SyncEndpointAsync(ep1.Id);

        var logs = await _db.SyncLogs.ToListAsync();
        logs.Should().OnlyContain(l => l.EndpointId == ep1.Id);
    }

    [Test]
    public async Task SyncEndpointAsync_DoesNothing_WhenEndpointNotFound()
    {
        _ipResolver.Setup(r => r.ResolveAsync(default)).ReturnsAsync("1.2.3.4");

        await _service.SyncEndpointAsync(Guid.NewGuid());

        var logs = await _db.SyncLogs.ToListAsync();
        logs.Should().BeEmpty();
    }

    [Test]
    public async Task SyncEndpointAsync_CreatesFailedLog_WhenIpResolutionFails()
    {
        var endpoint = await SeedEndpointAsync();
        _ipResolver.Setup(r => r.ResolveAsync(default)).ReturnsAsync((string?)null);

        await _service.SyncEndpointAsync(endpoint.Id);

        var log = await _db.SyncLogs.SingleAsync(l => l.EndpointId == endpoint.Id);
        log.Action.Should().Be(SyncAction.Failed);
    }

    // --- PurgeOldLogsAsync (via RunSyncAsync) ---

    [Test]
    public async Task RunSyncAsync_PurgesLogsOlderThanRetention()
    {
        _db.AppSettings.Add(new AppSettings { LogRetentionHours = 24 });
        var endpoint = await SeedEndpointAsync();
        _db.SyncLogs.Add(new SyncLog { EndpointId = endpoint.Id, Action = SyncAction.NoChange, Timestamp = DateTime.UtcNow.AddHours(-25) });
        _db.SyncLogs.Add(new SyncLog { EndpointId = endpoint.Id, Action = SyncAction.NoChange, Timestamp = DateTime.UtcNow.AddHours(-1) });
        await _db.SaveChangesAsync();

        _ipResolver.Setup(r => r.ResolveAsync(It.IsAny<CancellationToken>())).ReturnsAsync("1.2.3.4");
        _provider.Setup(p => p.UpsertRecordAsync(It.IsAny<ProviderAccount>(), It.IsAny<DnsEndpoint>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.RunSyncAsync();

        var remaining = await _db.SyncLogs.ToListAsync();
        remaining.Should().NotContain(l => l.Timestamp < DateTime.UtcNow.AddHours(-24));
    }

    [Test]
    public async Task RunSyncAsync_KeepsLogsWithinRetention()
    {
        _db.AppSettings.Add(new AppSettings { LogRetentionHours = 24 });
        var endpoint = await SeedEndpointAsync();
        _db.SyncLogs.Add(new SyncLog { EndpointId = endpoint.Id, Action = SyncAction.NoChange, Timestamp = DateTime.UtcNow.AddHours(-1) });
        await _db.SaveChangesAsync();

        _ipResolver.Setup(r => r.ResolveAsync(It.IsAny<CancellationToken>())).ReturnsAsync("1.2.3.4");
        _provider.Setup(p => p.UpsertRecordAsync(It.IsAny<ProviderAccount>(), It.IsAny<DnsEndpoint>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.RunSyncAsync();

        var remaining = await _db.SyncLogs.ToListAsync();
        remaining.Should().Contain(l => l.Timestamp > DateTime.UtcNow.AddHours(-2));
    }

    [Test]
    public async Task RunSyncAsync_LogsFailure_WhenIpResolutionFails()
    {
        _db.AppSettings.Add(new AppSettings());
        var endpoint = await SeedEndpointAsync();

        _ipResolver.Setup(r => r.ResolveAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        await _service.RunSyncAsync();

        var log = await _db.SyncLogs.SingleAsync(l => l.EndpointId == endpoint.Id);
        log.Action.Should().Be(SyncAction.Failed);
    }

    // --- helpers ---

    private async Task<ProviderAccount> SeedProviderAsync()
    {
        var p = new ProviderAccount { Name = "Test", ProviderType = "Route53", EncryptedCredentials = "{}" };
        _db.ProviderAccounts.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    private async Task<DnsEndpoint> SeedEndpointAsync(ProviderAccount? provider = null, string hostname = "test.example.com")
    {
        provider ??= await SeedProviderAsync();
        var endpoint = new DnsEndpoint { Hostname = hostname, ZoneName = "example.com", ProviderAccountId = provider.Id };
        _db.DnsEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();
        return endpoint;
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
