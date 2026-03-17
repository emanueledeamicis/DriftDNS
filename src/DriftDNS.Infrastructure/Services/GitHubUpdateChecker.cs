using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DriftDNS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DriftDNS.Infrastructure.Services;

public class GitHubUpdateChecker : IUpdateChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubUpdateChecker> _logger;

    private string? _cachedLatestVersion;
    private DateTime _lastCheckedAt = DateTime.MinValue;
    private static readonly TimeSpan CacheInterval = TimeSpan.FromHours(12);

    private const string ApiUrl = "https://api.github.com/repos/emanueledeamicis/DriftDNS/releases/latest";

    public GitHubUpdateChecker(IHttpClientFactory httpClientFactory, ILogger<GitHubUpdateChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - _lastCheckedAt < CacheInterval)
            return _cachedLatestVersion;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DriftDNS");

            var response = await client.GetFromJsonAsync<GitHubReleaseResponse>(ApiUrl, cancellationToken);
            var tag = response?.TagName?.TrimStart('v');

            _cachedLatestVersion = tag;
            _lastCheckedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates");
        }

        return _cachedLatestVersion;
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}
