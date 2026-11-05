using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class FetchSubscriptionEngineTests
{
    private const string Node1Plain =
        "vless://00000000-0000-0000-0000-000000000000@ad.small-biz.org:20001?type=xhttp&security=none#OriginalNode1";

    private const string Node2Reality =
        "vless://11111111-1111-1111-1111-111111111111@194.182.84.231:30001?type=xhttp&security=reality&sni=topicboard.app#OriginalNode2";

    [Theory]
    [InlineData("fetch")]
    [InlineData("FETCH")]
    [InlineData("Fetch")]
    public void CanHandle_ShouldReturnTrue_ForFetch(string engineName)
    {
        var engine = new FetchSubscriptionEngine(new FakeFetcher(), new FakeUriEditor());
        Assert.True(engine.CanHandle(engineName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("static")]
    [InlineData("other")]
    public void CanHandle_ShouldReturnFalse_ForNonFetch(string? engineName)
    {
        var engine = new FetchSubscriptionEngine(new FakeFetcher(), new FakeUriEditor());
        Assert.False(engine.CanHandle(engineName));
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldMatchBySniAndHost_AndApplyOverrides()
    {
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult([Node1Plain, Node2Reality]));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchSubscriptionEngine(fakeFetcher, fakeEditor);

        var client = new VpnConnection
        {
            ClientId = "dynamic-client",
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/test",
            Overrides =
            [
                new VpnConnectionOverride
                {
                    MatchSni = "topicboard.app",
                    Title = "Reality Fast",
                    Host = "reality.proxy.com",
                    Port = 443
                },
                new VpnConnectionOverride
                {
                    MatchHost = "ad.small-biz.org",
                    Title = "Plain CDN",
                    Host = "cdn.proxy.com",
                    Port = 8443
                }
            ]
        };

        var result = await engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Configs.Count);

        // Node 1 was matched by MatchHost
        Assert.Contains("cdn.proxy.com:8443", result.Configs[0]);
        Assert.Contains("#Plain CDN", result.Configs[0]);

        // Node 2 was matched by MatchSni
        Assert.Contains("reality.proxy.com:443", result.Configs[1]);
        Assert.Contains("#Reality Fast", result.Configs[1]);
        Assert.Contains("sni=topicboard.app", result.Configs[1]); // SNI untouched
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldPassThroughUnmatchedNodes_WithDefaultTitle()
    {
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult([Node1Plain, Node2Reality], "DefaultProfileTitle"));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchSubscriptionEngine(fakeFetcher, fakeEditor);

        var client = new VpnConnection
        {
            ClientId = "dynamic-client",
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/test",
            Overrides =
            [
                new VpnConnectionOverride
                {
                    MatchSni = "nonexistent.sni.com",
                    Host = "ignored.com"
                }
            ]
        };

        var result = await engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Configs.Count);
        // Original fragments preserved since no overrides matched
        Assert.Equal(Node1Plain, result.Configs[0]);
        Assert.Equal(Node2Reality, result.Configs[1]);
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldForwardHeaders_AndApplyFallbackInterval()
    {
        var headers = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=1000; download=2000; total=50000; expire=1735689600",
            [SubscriptionHeaders.Title] = "base64:VGVzdFRpdGxl"
        };
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult([Node1Plain], Headers: headers));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchSubscriptionEngine(fakeFetcher, fakeEditor);

        var client = new VpnConnection
        {
            ClientId = "test",
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/test"
        };

        var result = await engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Headers);
        Assert.Equal("upload=1000; download=2000; total=50000; expire=1735689600", result.Headers[SubscriptionHeaders.UserInfo]);
        Assert.Equal("base64:VGVzdFRpdGxl", result.Headers[SubscriptionHeaders.Title]);
        Assert.Equal(SubscriptionHeaders.DefaultUpdateInterval, result.Headers[SubscriptionHeaders.UpdateInterval]); // Fallback applied
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldPreserveUpstreamUpdateInterval_WhenProvided()
    {
        var headers = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=0; download=0; total=0; expire=0",
            [SubscriptionHeaders.UpdateInterval] = "6"
        };
        var fakeFetcher = new FakeFetcher(url => new SubscriptionResult([Node1Plain], Headers: headers));
        var fakeEditor = new FakeUriEditor();
        var engine = new FetchSubscriptionEngine(fakeFetcher, fakeEditor);

        var client = new VpnConnection
        {
            ClientId = "test",
            Engine = "fetch",
            SubscriptionUrl = "https://example.com/sub/test"
        };

        var result = await engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Headers);
        Assert.Equal("6", result.Headers[SubscriptionHeaders.UpdateInterval]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveConfigsAsync_ShouldReturnEmptyList_WhenSubscriptionUrlIsEmpty(string? invalidUrl)
    {
        var engine = new FetchSubscriptionEngine(new FakeFetcher(), new FakeUriEditor());

        var client = new VpnConnection
        {
            ClientId = "test",
            Engine = "fetch",
            SubscriptionUrl = invalidUrl
        };

        var result = await engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Empty(result.Configs);
    }

    private class FakeFetcher(Func<string, SubscriptionResult>? handler = null) : ISubscriptionFetcher
    {
        public Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
        {
            if (handler != null)
            {
                return Task.FromResult(handler(subscriptionUrl));
            }
            return Task.FromResult(new SubscriptionResult([Node1Plain, Node2Reality]));
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
