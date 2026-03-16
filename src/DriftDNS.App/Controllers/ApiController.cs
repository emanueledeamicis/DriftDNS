using DriftDNS.Core.Interfaces;
using DriftDNS.Core.Models;
using DriftDNS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriftDNS.App.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly DriftDnsDbContext _db;
    private readonly IPublicIpResolver _ipResolver;
    private readonly IDnsSyncService _syncService;

    public ApiController(DriftDnsDbContext db, IPublicIpResolver ipResolver, IDnsSyncService syncService)
    {
        _db = db;
        _ipResolver = ipResolver;
        _syncService = syncService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var ip = await _ipResolver.ResolveAsync();
        var lastSync = await _db.SyncLogs
            .OrderByDescending(l => l.Timestamp)
            .Select(l => (DateTime?)l.Timestamp)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            PublicIp = ip,
            LastSync = lastSync,
            EndpointCount = await _db.DnsEndpoints.CountAsync(),
        });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        await _syncService.RunSyncAsync(cancellationToken);
        return Ok(new { Message = "Sync completed" });
    }

    [HttpGet("endpoints")]
    public async Task<IActionResult> GetEndpoints() =>
        Ok(await _db.DnsEndpoints.Include(e => e.ProviderAccount).ToListAsync());

    [HttpPost("endpoints")]
    public async Task<IActionResult> CreateEndpoint([FromBody] DnsEndpoint endpoint)
    {
        endpoint.Id = Guid.NewGuid();
        endpoint.CreatedAt = DateTime.UtcNow;
        _db.DnsEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEndpoints), new { }, endpoint);
    }
}
