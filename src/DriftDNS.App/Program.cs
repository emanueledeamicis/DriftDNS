using DriftDNS.App.Workers;
using DriftDNS.Core.Interfaces;
using DriftDNS.Infrastructure.Data;
using DriftDNS.Infrastructure.Services;
using DriftDNS.Providers.Route53;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Database
var dbPath = builder.Configuration.GetValue<string>("DatabasePath") ?? "/app/data/app.db";
builder.Services.AddDbContext<DriftDnsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// HttpClient for IP resolver
builder.Services.AddHttpClient();

// Core services
builder.Services.AddScoped<IPublicIpResolver, PublicIpResolver>();
builder.Services.AddScoped<IDnsSyncService, DnsSyncService>();

// DNS providers
builder.Services.AddScoped<IDnsProvider, Route53DnsProvider>();

// Background worker
builder.Services.AddHostedService<DnsSyncWorker>();

// API controllers
builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DriftDnsDbContext>();
    db.Database.Migrate();

    if (!db.AppSettings.Any())
    {
        db.AppSettings.Add(new DriftDNS.Core.Models.AppSettings());
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

app.Run();
