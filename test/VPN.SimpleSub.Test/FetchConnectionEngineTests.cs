using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class FetchConnectionEngineTests
{
    private const string UpstreamVless =
        "vless://00000000-0000-0000-0000-000000000000@upstream.com:8443?type=xhttp";

    [Theory]
    [InlineData("fetch")]
    [InlineData("FETCH")]
    [InlineData("Fetch")]
    public void CanHandle_ShouldReturnTrue_ForFetch(string engineName)
    {
        var engine = new FetchConnectionEngine(new FakeFetcher(), new FakeUriEditor());
        Assert.True(engine.CanHandle(engineName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("static")]
    [InlineData("other")]
    public void CanHandle_ShouldReturnFalse_ForNonFetch(string? engineName)
    {
        var engine = new FetchConnectionEngine(new FakeFetcher(), new FakeUriEditor());
        Assert.False(engine.CanHandle(engineName));
    }

    [Fact]
    public async Task ResolveAsync_ShouldFetchAndApplyHostAndPortOverrides()
    {
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult(UpstreamVless, "UpstreamTitle"));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchConnectionEngine(fakeFetcher, fakeEditor);

        var connection = new VpnConnectionDetails
        {
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/1",
            Host = "cdn.example.com",
            Port = 443,
            Title = "My Custom Title"
        };

        var result = await engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("cdn.example.com:443", result);
        Assert.Contains("#My Custom Title", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUpdatePort_WhenPortIsSpecifiedButHostIsNot()
    {
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult(UpstreamVless));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchConnectionEngine(fakeFetcher, fakeEditor);

        var connection = new VpnConnectionDetails
        {
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/1",
            Port = 9443
        };

        var result = await engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("upstream.com:9443", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldFallbackToProfileTitle_WhenConnectionTitleNotProvided()
    {
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult(UpstreamVless, "UpstreamTitle"));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchConnectionEngine(fakeFetcher, fakeEditor);

        var connection = new VpnConnectionDetails
        {
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/1"
        };

        var result = await engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("#UpstreamTitle", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_ShouldReturnNull_WhenSubscriptionUrlIsEmpty(string? invalidUrl)
    {
        var engine = new FetchConnectionEngine(new FakeFetcher(), new FakeUriEditor());

        var connection = new VpnConnectionDetails
        {
            Engine = "fetch",
            SubscriptionUrl = invalidUrl
        };

        var result = await engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private class FakeFetcher(Func<string, SubscriptionResult>? handler = null) : ISubscriptionFetcher
    {
        public Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
        {
            if (handler != null)
            {
                return Task.FromResult(handler(subscriptionUrl));
            }
            return Task.FromResult(new SubscriptionResult(UpstreamVless));
        }
    }

    private class FakeUriEditor : IUriEditor
    {
        public string SetHostAndPort(string uri, string? host = null, int? port = null)
        {
            var builder = new UriBuilder(uri);
            if (!string.IsNullOrWhiteSpace(host))
            {
                builder.Host = host;
            }
            if (port.HasValue)
            {
                builder.Port = port.Value;
            }
            return builder.Uri.ToString();
        }

        public string SetQueryParameter(string uri, string key, string value) => uri;
        public string RemoveQueryParameter(string uri, string key) => uri;
    }
}
