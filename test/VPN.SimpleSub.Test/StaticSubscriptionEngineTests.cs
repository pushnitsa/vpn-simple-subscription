using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class StaticSubscriptionEngineTests
{
    private readonly StaticSubscriptionEngine _engine = new(new FakeUriEditor());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("static")]
    [InlineData("STATIC")]
    public void CanHandle_ShouldReturnTrue_ForNullEmptyOrStatic(string? engineName)
    {
        Assert.True(_engine.CanHandle(engineName));
    }

    [Theory]
    [InlineData("fetch")]
    [InlineData("unknown")]
    public void CanHandle_ShouldReturnFalse_ForOtherEngines(string engineName)
    {
        Assert.False(_engine.CanHandle(engineName));
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldReturnFormattedLines_ForValidConnections()
    {
        var client = new VpnConnection
        {
            ClientId = "test",
            Connections =
            [
                new VpnConnectionDetails
                {
                    ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp",
                    Title = "Node 1"
                },
                new VpnConnectionDetails
                {
                    ConnectionString = "vless://11111111-1111-1111-1111-111111111111@example.com:443?type=ws",
                    Host = "custom.example.com",
                    Port = 8443,
                    Title = "Node 2"
                }
            ]
        };

        var result = await _engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Configs.Count);
        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#Node 1", result.Configs[0]);
        Assert.Contains("custom.example.com:8443", result.Configs[1]);
        Assert.Contains("#Node 2", result.Configs[1]);
        Assert.NotNull(result.Headers);
        Assert.Equal(SubscriptionHeaders.DefaultUpdateInterval, result.Headers[SubscriptionHeaders.UpdateInterval]);
        Assert.False(result.Headers.ContainsKey(SubscriptionHeaders.UserInfo));
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldSkipEmptyConnectionStrings()
    {
        var client = new VpnConnection
        {
            ClientId = "test",
            Connections =
            [
                new VpnConnectionDetails { ConnectionString = "" },
                new VpnConnectionDetails { ConnectionString = "   " },
                new VpnConnectionDetails { ConnectionString = null }
            ]
        };

        var result = await _engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Empty(result.Configs);
    }

    [Fact]
    public async Task ResolveConfigsAsync_ShouldReturnEmptyList_WhenConnectionsIsNull()
    {
        var client = new VpnConnection
        {
            ClientId = "test",
            Connections = null
        };

        var result = await _engine.ResolveConfigsAsync(client, TestContext.Current.CancellationToken);

        Assert.Empty(result.Configs);
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
        public IReadOnlyDictionary<string, string> GetQueryParameters(string uri) => new Dictionary<string, string>();
    }
}
