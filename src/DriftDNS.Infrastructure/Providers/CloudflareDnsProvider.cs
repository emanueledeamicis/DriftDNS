using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DriftDNS.Core.Interfaces;
using DriftDNS.Core.Models;
using DriftDNS.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace DriftDNS.Infrastructure.Providers;

public class CloudflareDnsProvider : IDnsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudflareDnsProvider> _logger;
    private readonly ICredentialProtector _credentialProtector;

    private const string BaseUrl = "https://api.cloudflare.com/client/v4";

    public string Name => "Cloudflare";

    public CloudflareDnsProvider(IHttpClientFactory httpClientFactory, ILogger<CloudflareDnsProvider> logger, ICredentialProtector credentialProtector)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _credentialProtector = credentialProtector;
    }

    public async Task ValidateCredentialsAsync(ProviderAccount account, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var response = await client.GetFromJsonAsync<CloudflareResponse<List<Zone>>>(
            $"{BaseUrl}/zones?per_page=1", cancellationToken);
        EnsureSuccess(response);
    }

    public async Task<IReadOnlyList<string>> ListZonesAsync(ProviderAccount account, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zones = new List<string>();
        int page = 1;

        while (true)
        {
            var response = await client.GetFromJsonAsync<CloudflareResponse<List<Zone>>>(
                $"{BaseUrl}/zones?per_page=50&page={page}", cancellationToken);
            EnsureSuccess(response);

            zones.AddRange(response!.Result!.Select(z => z.Name));

            if (page >= response.ResultInfo!.TotalPages) break;
            page++;
        }

        return zones;
    }

    public async Task<IReadOnlyList<string>> ListHostnamesAsync(ProviderAccount account, string zoneName, string recordType, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zoneId = await ResolveZoneIdAsync(client, zoneName, cancellationToken);
        var hostnames = new List<string>();
        int page = 1;

        while (true)
        {
            var response = await client.GetFromJsonAsync<CloudflareResponse<List<DnsRecord>>>(
                $"{BaseUrl}/zones/{zoneId}/dns_records?type={recordType}&per_page=100&page={page}", cancellationToken);
            EnsureSuccess(response);

            hostnames.AddRange(response!.Result!.Select(r => r.Name));

            if (page >= response.ResultInfo!.TotalPages) break;
            page++;
        }

        return hostnames;
    }

    public async Task UpsertRecordAsync(ProviderAccount account, DnsEndpoint endpoint, string ipAddress, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zoneId = await ResolveZoneIdAsync(client, endpoint.ZoneName, cancellationToken);

        var existing = await FindRecordAsync(client, zoneId, endpoint.Hostname, endpoint.RecordType, cancellationToken);

        if (existing is null)
            throw new InvalidOperationException(
                $"DNS record '{endpoint.Hostname}' ({endpoint.RecordType}) not found in Cloudflare. " +
                "Create it manually on the provider, then DriftDNS will keep it in sync.");

        var body = new
        {
            type = endpoint.RecordType,
            name = endpoint.Hostname,
            content = ipAddress,
            ttl = existing.Ttl,
            proxied = existing.Proxied
        };

        _logger.LogInformation("Updating Cloudflare record {Hostname} → {Ip}", endpoint.Hostname, ipAddress);
        var response = await client.PutAsJsonAsync($"{BaseUrl}/zones/{zoneId}/dns_records/{existing.Id}", body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int?> VerifyRecordAsync(ProviderAccount account, DnsEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zoneId = await ResolveZoneIdAsync(client, endpoint.ZoneName, cancellationToken);
        var record = await FindRecordAsync(client, zoneId, endpoint.Hostname, endpoint.RecordType, cancellationToken);
        return record?.Ttl;
    }

    private async Task<string> ResolveZoneIdAsync(HttpClient client, string zoneName, CancellationToken cancellationToken)
    {
        var response = await client.GetFromJsonAsync<CloudflareResponse<List<Zone>>>(
            $"{BaseUrl}/zones?name={Uri.EscapeDataString(zoneName)}&per_page=1", cancellationToken);
        EnsureSuccess(response);

        var zone = response!.Result!.FirstOrDefault()
            ?? throw new InvalidOperationException($"Zone '{zoneName}' not found in Cloudflare");

        return zone.Id;
    }

    private async Task<DnsRecord?> FindRecordAsync(HttpClient client, string zoneId, string hostname, string recordType, CancellationToken cancellationToken)
    {
        var response = await client.GetFromJsonAsync<CloudflareResponse<List<DnsRecord>>>(
            $"{BaseUrl}/zones/{zoneId}/dns_records?type={recordType}&name={Uri.EscapeDataString(hostname)}", cancellationToken);
        EnsureSuccess(response);
        return response!.Result!.FirstOrDefault();
    }

    private HttpClient CreateClient(ProviderAccount account)
    {
        var apiToken = ParseCredentials(_credentialProtector.Unprotect(account.EncryptedCredentials));
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static void EnsureSuccess<T>(CloudflareResponse<T>? response)
    {
        if (response is null || !response.Success)
        {
            var errors = response?.Errors is { Count: > 0 }
                ? string.Join(", ", response.Errors.Select(e => e.Message))
                : "Unknown Cloudflare API error";
            throw new InvalidOperationException(errors);
        }
    }

    private static string ParseCredentials(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("ApiToken").GetString()
            ?? throw new InvalidOperationException("Missing ApiToken");
    }

    private sealed class CloudflareResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("errors")] public List<CloudflareError>? Errors { get; set; }
        [JsonPropertyName("result")] public T? Result { get; set; }
        [JsonPropertyName("result_info")] public ResultInfo? ResultInfo { get; set; }
    }

    private sealed class CloudflareError
    {
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class ResultInfo
    {
        [JsonPropertyName("total_pages")] public int TotalPages { get; set; }
    }

    private sealed class Zone
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }

    private sealed class DnsRecord
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("ttl")] public int Ttl { get; set; }
        [JsonPropertyName("proxied")] public bool Proxied { get; set; }
    }
}
