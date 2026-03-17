using DriftDNS.Core.Models;
using FluentAssertions;
using NUnit.Framework;

namespace DriftDNS.Tests;

[TestFixture]
public class SyncLogTests
{
    [Test]
    public void SyncLog_DefaultAction_IsNoChange()
    {
        var log = new SyncLog();
        log.Action.Should().Be(SyncAction.NoChange);
    }

    [Test]
    public void SyncLog_Id_IsGenerated()
    {
        var log = new SyncLog();
        log.Id.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void ProviderAccount_IsEnabled_DefaultsTrue()
    {
        var account = new ProviderAccount();
        account.IsEnabled.Should().BeTrue();
    }

    [Test]
    public void DnsEndpoint_RecordType_DefaultsToA()
    {
        var endpoint = new DnsEndpoint();
        endpoint.RecordType.Should().Be("A");
    }

    [Test]
    public void AppSettings_LogRetentionHours_DefaultsTwentyFour()
    {
        var settings = new AppSettings();
        settings.LogRetentionHours.Should().Be(24);
    }
}
