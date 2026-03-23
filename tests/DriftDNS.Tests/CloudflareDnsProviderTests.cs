using DriftDNS.Core.Models;
using DriftDNS.Providers.Cloudflare;
using DriftDNS.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace DriftDNS.Tests;

[TestFixture]
public class CloudflareDnsProviderTests
{
    private FakeHttpMessageHandler _handler = null!;
    private CloudflareDnsProvider _provider = null!;
    private ProviderAccount _account = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new FakeHttpMessageHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_handler));

        _provider = new CloudflareDnsProvider(factory.Object, Mock.Of<ILogger<CloudflareDnsProvider>>());
        _account = new ProviderAccount
        {
            Name = "Test",
            ProviderType = "Cloudflare",
            EncryptedCredentials = JsonSerializer.Serialize(new { ApiToken = "fake-token" })
        };
    }

    // --- ValidateCredentials ---

    [Test]
    public async Task ValidateCredentials_DoesNotThrow_WhenApiReturnsSuccess()
    {
        _handler.EnqueueJson(CloudflareSuccess(new object[] { }));

        var act = async () => await _provider.ValidateCredentialsAsync(_account);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ValidateCredentials_Throws_WhenApiReturnsFailure()
    {
        _handler.EnqueueJson(new { success = false, errors = new[] { new { message = "Invalid token" } }, result = (object?)null, result_info = new { total_pages = 0 } });

        var act = async () => await _provider.ValidateCredentialsAsync(_account);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid token*");
    }

    // --- ListZones ---

    [Test]
    public async Task ListZones_ReturnsZoneNames()
    {
        _handler.EnqueueJson(CloudflareSuccess(new[]
        {
            new { id = "zone-1", name = "example.com" },
            new { id = "zone-2", name = "example.org" }
        }));

        var zones = await _provider.ListZonesAsync(_account);

        zones.Should().BeEquivalentTo(["example.com", "example.org"]);
    }

    [Test]
    public async Task ListZones_ReturnsEmpty_WhenNoZones()
    {
        _handler.EnqueueJson(CloudflareSuccess(new object[] { }));

        var zones = await _provider.ListZonesAsync(_account);

        zones.Should().BeEmpty();
    }

    // --- ListHostnames ---

    [Test]
    public async Task ListHostnames_ReturnsHostnames()
    {
        // resolve zone
        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "zone-1", name = "example.com" } }));
        // list records
        _handler.EnqueueJson(CloudflareSuccess(new[]
        {
            new { id = "rec-1", name = "sub.example.com" },
            new { id = "rec-2", name = "www.example.com" }
        }));

        var hostnames = await _provider.ListHostnamesAsync(_account, "example.com", "A");

        hostnames.Should().BeEquivalentTo(["sub.example.com", "www.example.com"]);
    }

    // --- UpsertRecord ---

    [Test]
    public async Task UpsertRecord_Throws_WhenRecordDoesNotExist()
    {
        var endpoint = BuildEndpoint();

        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "zone-1", name = "example.com" } })); // resolve zone
        _handler.EnqueueJson(CloudflareSuccess(new object[] { }));                                       // find record → not found

        var act = async () => await _provider.UpsertRecordAsync(_account, endpoint, "1.2.3.4");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        _handler.CallCount.Should().Be(2);
    }

    [Test]
    public async Task UpsertRecord_UpdatesRecord_WhenRecordExists()
    {
        var endpoint = BuildEndpoint();

        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "zone-1", name = "example.com" } }));            // resolve zone
        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "rec-existing", name = "test.example.com" } })); // find record → found
        _handler.EnqueueJson(CloudflareSuccess(new { id = "rec-existing" }));                                       // PUT update

        var act = async () => await _provider.UpsertRecordAsync(_account, endpoint, "1.2.3.4");

        await act.Should().NotThrowAsync();
        _handler.CallCount.Should().Be(3);
    }

    [Test]
    public async Task UpsertRecord_Throws_WhenZoneNotFound()
    {
        var endpoint = BuildEndpoint();

        _handler.EnqueueJson(CloudflareSuccess(new object[] { })); // zone not found

        var act = async () => await _provider.UpsertRecordAsync(_account, endpoint, "1.2.3.4");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*example.com*");
    }

    // --- VerifyRecord ---

    [Test]
    public async Task VerifyRecord_ReturnsTrue_WhenRecordExists()
    {
        var endpoint = BuildEndpoint();

        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "zone-1", name = "example.com" } }));            // resolve zone
        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "rec-1", name = "test.example.com" } }));        // find record → found

        var result = await _provider.VerifyRecordAsync(_account, endpoint);

        result.Should().BeTrue();
    }

    [Test]
    public async Task VerifyRecord_ReturnsFalse_WhenRecordDoesNotExist()
    {
        var endpoint = BuildEndpoint();

        _handler.EnqueueJson(CloudflareSuccess(new[] { new { id = "zone-1", name = "example.com" } })); // resolve zone
        _handler.EnqueueJson(CloudflareSuccess(new object[] { }));                                       // find record → not found

        var result = await _provider.VerifyRecordAsync(_account, endpoint);

        result.Should().BeFalse();
    }

    // --- helpers ---

    private static DnsEndpoint BuildEndpoint() => new()
    {
        Hostname = "test.example.com",
        ZoneName = "example.com",
        RecordType = "A",
        TTL = 300
    };

    private static object CloudflareSuccess<T>(T result) => new
    {
        success = true,
        errors = Array.Empty<object>(),
        result,
        result_info = new { total_pages = 1 }
    };
}
