using DriftDNS.Infrastructure.Services;
using DriftDNS.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DriftDNS.Tests;

[TestFixture]
public class GitHubUpdateCheckerTests
{
    private FakeHttpMessageHandler _handler = null!;
    private GitHubUpdateChecker _checker = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new FakeHttpMessageHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_handler));

        _checker = new GitHubUpdateChecker(factory.Object, Mock.Of<ILogger<GitHubUpdateChecker>>());
    }

    [Test]
    public async Task GetLatestVersion_ReturnsVersion_WhenApiSucceeds()
    {
        _handler.EnqueueJson(new { tag_name = "v0.2.0" });

        var version = await _checker.GetLatestVersionAsync();

        version.Should().Be("0.2.0");
    }

    [Test]
    public async Task GetLatestVersion_StripsLeadingV_FromTag()
    {
        _handler.EnqueueJson(new { tag_name = "v1.5.3" });

        var version = await _checker.GetLatestVersionAsync();

        version.Should().Be("1.5.3");
    }

    [Test]
    public async Task GetLatestVersion_ReturnsNull_WhenApiFails()
    {
        _handler.EnqueueError();

        var version = await _checker.GetLatestVersionAsync();

        version.Should().BeNull();
    }

    [Test]
    public async Task GetLatestVersion_ReturnsCachedValue_OnSubsequentCalls()
    {
        _handler.EnqueueJson(new { tag_name = "v0.2.0" });

        await _checker.GetLatestVersionAsync();
        var version = await _checker.GetLatestVersionAsync();

        version.Should().Be("0.2.0");
        _handler.CallCount.Should().Be(1);
    }
}
