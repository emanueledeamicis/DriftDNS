using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using DriftDNS.Core.Interfaces;
using DriftDNS.Core.Models;
using DriftDNS.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DriftDNS.Infrastructure.Providers;

public class Route53DnsProvider : IDnsProvider
{
    private readonly ILogger<Route53DnsProvider> _logger;
    private readonly ICredentialProtector _credentialProtector;

    public string Name => "Route53";

    public Route53DnsProvider(ILogger<Route53DnsProvider> logger, ICredentialProtector credentialProtector)
    {
        _logger = logger;
        _credentialProtector = credentialProtector;
    }

    public async Task ValidateCredentialsAsync(ProviderAccount account, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        await client.ListHostedZonesByNameAsync(new ListHostedZonesByNameRequest { MaxItems = "1" }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListZonesAsync(ProviderAccount account, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zones = new List<string>();
        string? nextDnsName = null;
        string? nextZoneId = null;

        do
        {
            var request = new ListHostedZonesByNameRequest();
            if (nextDnsName is not null) request.DNSName = nextDnsName;
            if (nextZoneId is not null) request.HostedZoneId = nextZoneId;

            var response = await client.ListHostedZonesByNameAsync(request, cancellationToken);
            zones.AddRange(response.HostedZones.Select(z => z.Name.TrimEnd('.')));

            if (response.IsTruncated == true)
            {
                nextDnsName = response.NextDNSName;
                nextZoneId = response.NextHostedZoneId;
            }
            else break;
        }
        while (true);

        return zones;
    }

    public async Task<IReadOnlyList<string>> ListHostnamesAsync(ProviderAccount account, string zoneName, string recordType, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zoneId = await ResolveZoneIdAsync(client, zoneName, cancellationToken);
        var rrType = new RRType(recordType);
        var hostnames = new List<string>();
        string? nextName = null;
        string? nextType = null;
        string? nextId = null;

        do
        {
            var request = new ListResourceRecordSetsRequest { HostedZoneId = zoneId };
            if (nextName is not null) request.StartRecordName = nextName;
            if (nextType is not null) request.StartRecordType = new RRType(nextType);
            if (nextId is not null) request.StartRecordIdentifier = nextId;

            var response = await client.ListResourceRecordSetsAsync(request, cancellationToken);

            hostnames.AddRange(response.ResourceRecordSets
                .Where(r => r.Type == rrType)
                .Select(r => r.Name.TrimEnd('.')));

            if (response.IsTruncated == true)
            {
                nextName = response.NextRecordName;
                nextType = response.NextRecordType?.Value;
                nextId = response.NextRecordIdentifier;
            }
            else break;
        }
        while (true);

        return hostnames;
    }

    public async Task UpsertRecordAsync(
        ProviderAccount account,
        DnsEndpoint endpoint,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);

        var zoneId = await ResolveZoneIdAsync(client, endpoint.ZoneName, cancellationToken);

        var existing = await FindRecordAsync(client, zoneId, endpoint.Hostname, endpoint.RecordType, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException(
                $"DNS record '{endpoint.Hostname}' ({endpoint.RecordType}) not found in Route53. " +
                "Create it manually on the provider, then DriftDNS will keep it in sync.");

        var request = new ChangeResourceRecordSetsRequest
        {
            HostedZoneId = zoneId,
            ChangeBatch = new ChangeBatch
            {
                Changes =
                [
                    new Change
                    {
                        Action = ChangeAction.UPSERT,
                        ResourceRecordSet = new ResourceRecordSet
                        {
                            Name = endpoint.Hostname,
                            Type = new RRType(endpoint.RecordType),
                            TTL = existing.TTL,
                            ResourceRecords = [new ResourceRecord { Value = ipAddress }],
                        },
                    },
                ],
            },
        };

        _logger.LogInformation("Upserting Route53 record {Hostname} → {Ip}", endpoint.Hostname, ipAddress);
        await client.ChangeResourceRecordSetsAsync(request, cancellationToken);
    }

    public async Task<int?> VerifyRecordAsync(ProviderAccount account, DnsEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(account);
        var zoneId = await ResolveZoneIdAsync(client, endpoint.ZoneName, cancellationToken);
        var record = await FindRecordAsync(client, zoneId, endpoint.Hostname, endpoint.RecordType, cancellationToken);
        return record is not null ? (int)record.TTL : null;
    }

    private static async Task<ResourceRecordSet?> FindRecordAsync(IAmazonRoute53 client, string zoneId, string hostname, string recordType, CancellationToken cancellationToken)
    {
        var name = hostname.TrimEnd('.') + '.';
        var response = await client.ListResourceRecordSetsAsync(new ListResourceRecordSetsRequest
        {
            HostedZoneId = zoneId,
            StartRecordName = name,
            StartRecordType = new RRType(recordType),
            MaxItems = "1"
        }, cancellationToken);

        return response.ResourceRecordSets.FirstOrDefault(r =>
            r.Name.TrimEnd('.').Equals(hostname.TrimEnd('.'), StringComparison.OrdinalIgnoreCase) &&
            r.Type == new RRType(recordType));
    }

    private static async Task<string> ResolveZoneIdAsync(IAmazonRoute53 client, string zoneName, CancellationToken cancellationToken)
    {
        var name = zoneName.TrimEnd('.') + '.';
        var response = await client.ListHostedZonesByNameAsync(
            new ListHostedZonesByNameRequest { DNSName = name, MaxItems = "1" },
            cancellationToken);

        var zone = response.HostedZones.FirstOrDefault(z =>
            z.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (zone is null)
            throw new InvalidOperationException($"Hosted zone '{zoneName}' not found in Route53");

        return zone.Id;
    }

    private AmazonRoute53Client CreateClient(ProviderAccount account)
    {
        var creds = ParseCredentials(_credentialProtector.Unprotect(account.EncryptedCredentials));
        var credentials = new BasicAWSCredentials(creds.AccessKey, creds.SecretKey);
        return new AmazonRoute53Client(credentials, RegionEndpoint.USEast1);
    }

    private static (string AccessKey, string SecretKey) ParseCredentials(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return (
            root.GetProperty("AccessKey").GetString() ?? throw new InvalidOperationException("Missing AccessKey"),
            root.GetProperty("SecretKey").GetString() ?? throw new InvalidOperationException("Missing SecretKey")
        );
    }
}
