using System.Net;
using DriftDNS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DriftDNS.Infrastructure.Services;

public class PublicIpResolver : IPublicIpResolver
{
    private static readonly string[] Sources =
    [
        "https://checkip.amazonaws.com",
        "https://api.ipify.org",
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicIpResolver> _logger;

    public PublicIpResolver(IHttpClientFactory httpClientFactory, ILogger<PublicIpResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        foreach (var source in Sources)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync(source, cancellationToken);
                var ip = response.Trim();

                if (IPAddress.TryParse(ip, out var parsed) && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    _logger.LogDebug("Resolved public IP {Ip} from {Source}", ip, source);
                    return ip;
                }

                _logger.LogWarning("Source {Source} returned invalid IP: {Response}", source, response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve public IP from {Source}", source);
            }
        }

        _logger.LogError("All public IP sources failed");
        return null;
    }
}
