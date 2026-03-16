using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriftDNS.Infrastructure.Data;

public class DriftDnsDbContextFactory : IDesignTimeDbContextFactory<DriftDnsDbContext>
{
    public DriftDnsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DriftDnsDbContext>()
            .UseSqlite("Data Source=app.db")
            .Options;

        return new DriftDnsDbContext(options);
    }
}
