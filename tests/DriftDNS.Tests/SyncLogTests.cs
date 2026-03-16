using DriftDNS.Core.Models;

namespace DriftDNS.Tests;

public class SyncLogTests
{
    [Fact]
    public void SyncLog_DefaultAction_IsNoChange()
    {
        var log = new SyncLog();
        Assert.Equal(SyncAction.NoChange, log.Action);
    }

    [Fact]
    public void SyncLog_Id_IsGenerated()
    {
        var log = new SyncLog();
        Assert.NotEqual(Guid.Empty, log.Id);
    }

    [Fact]
    public void ProviderAccount_IsEnabled_DefaultsTrue()
    {
        var account = new ProviderAccount();
        Assert.True(account.IsEnabled);
    }

    [Fact]
    public void DnsEndpoint_RecordType_DefaultsToA()
    {
        var endpoint = new DnsEndpoint();
        Assert.Equal("A", endpoint.RecordType);
    }
}
