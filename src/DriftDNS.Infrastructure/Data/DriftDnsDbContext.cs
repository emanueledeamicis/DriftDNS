using DriftDNS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DriftDNS.Infrastructure.Data;

public class DriftDnsDbContext : DbContext
{
    public DriftDnsDbContext(DbContextOptions<DriftDnsDbContext> options) : base(options) { }

    public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();
    public DbSet<DnsEndpoint> DnsEndpoints => Set<DnsEndpoint>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncState>()
            .HasKey(s => s.EndpointId);

        modelBuilder.Entity<SyncState>()
            .HasOne(s => s.Endpoint)
            .WithOne(e => e.SyncState)
            .HasForeignKey<SyncState>(s => s.EndpointId);

        modelBuilder.Entity<DnsEndpoint>()
            .HasOne(e => e.ProviderAccount)
            .WithMany(p => p.DnsEndpoints)
            .HasForeignKey(e => e.ProviderAccountId);

        modelBuilder.Entity<SyncLog>()
            .HasOne(l => l.Endpoint)
            .WithMany(e => e.SyncLogs)
            .HasForeignKey(l => l.EndpointId);
    }
}
