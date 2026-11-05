using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class SubscriptionServiceTests : IDisposable
{
    private readonly string _tempFilePath;

    public SubscriptionServiceTests()
    {
        _tempFilePath = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    private SubscriptionService CreateService(
        List<VpnConnection> connections,
        ISubscriptionFetcher? fetcher = null,
        IUriEditor? uriEditor = null)
    {
        var json = JsonSerializer.Serialize(connections);
        File.WriteAllText(_tempFilePath, json);

        var options = Options.Create(new AppSettings
        {
            SubscriptionStorageDiscoveryPath = _tempFilePath,
            SubscriptionRoute = "sub"
        });

        return new SubscriptionService(
            options,
            fetcher ?? new FakeSubscriptionFetcher(),
            uriEditor ?? new FakeUriEditor());
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldReturnBase64Joined_ForStaticConnections()
    {
        // Arrange
        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client1",
                Connections =
                [
                    new VpnConnectionDetails
                    {
                        Engine = "static",
                        Title = "Node 1",
                        ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp"
                    },
                    new VpnConnectionDetails
                    {
                        // Implicit default engine
                        Title = "Node 2",
                        ConnectionString = "vless://11111111-1111-1111-1111-111111111111@example.com:443?type=ws"
                    }
                ]
            }
        };

        var service = CreateService(connections);

        // Act
        var base64Result = await service.GetSubscriptionAsync("client1");

        // Assert
        Assert.NotNull(base64Result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Result));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#Node 1", lines[0]);
        Assert.Equal("vless://11111111-1111-1111-1111-111111111111@example.com:443?type=ws#Node 2", lines[1]);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldFetchAndApplyHostOverride_ForFetchEngine()
    {
        // Arrange
        const string upstreamVless = "vless://22222222-2222-2222-2222-222222222222@upstream.com:8443?type=xhttp";
        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client2",
                Connections =
                [
                    new VpnConnectionDetails
                    {
                        Engine = "fetch",
                        Title = "Dynamic Node",
                        SubscriptionUrl = "https://upstream.example.com/sub/test",
                        Host = "cdn.example.com",
                        Port = 443
                    }
                ]
            }
        };

        var fakeFetcher = new FakeSubscriptionFetcher(url => new SubscriptionResult(upstreamVless));
        var fakeEditor = new FakeUriEditor();
        var service = CreateService(connections, fakeFetcher, fakeEditor);

        // Act
        var base64Result = await service.GetSubscriptionAsync("client2");

        // Assert
        Assert.NotNull(base64Result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Result));
        Assert.Contains("cdn.example.com:443", decoded);
        Assert.Contains("#Dynamic Node", decoded);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldHandleHybridConnections_MixingStaticAndFetch()
    {
        // Arrange
        const string upstreamVless = "vless://33333333-3333-3333-3333-333333333333@upstream.com:443?type=xhttp";
        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "hybrid-client",
                Connections =
                [
                    new VpnConnectionDetails
                    {
                        Engine = "static",
                        Title = "Static Node",
                        ConnectionString = "vless://00000000-0000-0000-0000-000000000000@static.com:443?type=tcp"
                    },
                    new VpnConnectionDetails
                    {
                        Engine = "fetch",
                        Title = "Remote Node",
                        SubscriptionUrl = "https://upstream.example.com/sub/hybrid",
                        Host = "proxy.example.com",
                        Port = 8443
                    }
                ]
            }
        };

        var fakeFetcher = new FakeSubscriptionFetcher(url => new SubscriptionResult(upstreamVless));
        var fakeEditor = new FakeUriEditor();
        var service = CreateService(connections, fakeFetcher, fakeEditor);

        // Act
        var base64Result = await service.GetSubscriptionAsync("hybrid-client");

        // Assert
        Assert.NotNull(base64Result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Result));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@static.com:443?type=tcp#Static Node", lines[0]);
        Assert.Contains("proxy.example.com:8443", lines[1]);
        Assert.Contains("type=xhttp#Remote Node", lines[1]);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldReturnNull_WhenClientIdNotFound()
    {
        // Arrange
        var service = CreateService([]);

        // Act
        var result = await service.GetSubscriptionAsync("unknown");

        // Assert
        Assert.Null(result);
    }

    private class FakeSubscriptionFetcher(Func<string, SubscriptionResult>? handler = null) : ISubscriptionFetcher
    {
        public Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
        {
            if (handler != null)
            {
                return Task.FromResult(handler(subscriptionUrl));
            }

            return Task.FromResult(new SubscriptionResult("vless://00000000-0000-0000-0000-000000000000@example.com:443"));
        }
    }

    private class FakeUriEditor : IUriEditor
    {
        public string SetHostAndPort(string uri, string host, int? port = null)
        {
            var builder = new UriBuilder(uri)
            {
                Host = host
            };
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
