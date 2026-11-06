using System.Text;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class SubscriptionServiceTests
{
    private static SubscriptionService CreateService(
        List<VpnConnection> connections,
        IEnumerable<ISubscriptionEngine>? engines = null)
    {
        var options = Options.Create(new AppSettings
        {
            SubscriptionStorageDiscoveryPath = "dummy-path",
            SubscriptionRoute = "sub"
        });

        var document = new SubscriptionsDocument { Subscriptions = connections };
        var reader = new FakeDocumentReader(document);

        return new SubscriptionService(
            options,
            engines ?? [new StaticSubscriptionEngine(new FakeUriEditor())],
            reader);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldReturnBase64Joined_ForStaticProfile()
    {
        // Arrange
        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client1",
                Engine = "static",
                Connections =
                [
                    new VpnConnectionDetails
                    {
                        Title = "Node 1",
                        ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp"
                    },
                    new VpnConnectionDetails
                    {
                        Title = "Node 2",
                        ConnectionString = "vless://11111111-1111-1111-1111-111111111111@example.com:443?type=ws"
                    }
                ]
            }
        };

        var service = CreateService(connections);

        // Act
        var result = await service.GetSubscriptionAsync("client1");

        // Assert
        Assert.NotNull(result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(result.Content));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#Node 1", lines[0]);
        Assert.Equal("vless://11111111-1111-1111-1111-111111111111@example.com:443?type=ws#Node 2", lines[1]);
        Assert.True(result.Headers.ContainsKey(SubscriptionHeaders.UpdateInterval));
        Assert.Equal(SubscriptionHeaders.DefaultUpdateInterval, result.Headers[SubscriptionHeaders.UpdateInterval]);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldDelegateToMatchingEngine_ForFetchProfile()
    {
        // Arrange
        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client2",
                Engine = "fetch",
                SubscriptionUrl = "https://example.com/sub/test"
            }
        };

        var headers = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=100; download=200; total=1000; expire=0",
            [SubscriptionHeaders.UpdateInterval] = "24"
        };
        var fakeEngine = new FakeEngine("fetch", ["vless://resolved-node-1#Test1", "vless://resolved-node-2#Test2"], headers);
        var service = CreateService(connections, [fakeEngine]);

        // Act
        var result = await service.GetSubscriptionAsync("client2");

        // Assert
        Assert.NotNull(result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(result.Content));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("vless://resolved-node-1#Test1", lines[0]);
        Assert.Equal("vless://resolved-node-2#Test2", lines[1]);
        Assert.Equal("upload=100; download=200; total=1000; expire=0", result.Headers[SubscriptionHeaders.UserInfo]);
        Assert.Equal("24", result.Headers[SubscriptionHeaders.UpdateInterval]);
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

    private class FakeDocumentReader(SubscriptionsDocument? document) : ISubscriptionDocumentReader
    {
        public Task<SubscriptionsDocument?> ReadAsync(string discoveryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(document);
    }

    private class FakeEngine(string engineName, List<string> resolvedLines, Dictionary<string, string>? headers = null) : ISubscriptionEngine
    {
        public bool CanHandle(string? engine) =>
            string.Equals(engine, engineName, StringComparison.OrdinalIgnoreCase);

        public Task<EngineSubscriptionResult> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EngineSubscriptionResult(resolvedLines, headers));
    }

    private class FakeUriEditor : IUriEditor
    {
        public string SetHostAndPort(string uri, string? host = null, int? port = null) => uri;
        public string SetQueryParameter(string uri, string key, string value) => uri;
        public string RemoveQueryParameter(string uri, string key) => uri;
        public IReadOnlyDictionary<string, string> GetQueryParameters(string uri) => new Dictionary<string, string>();
    }
}
