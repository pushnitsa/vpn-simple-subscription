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
        IEnumerable<ISubscriptionEngine>? engines = null)
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
            engines ?? [new StaticSubscriptionEngine(new FakeUriEditor())]);
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

        var fakeEngine = new FakeEngine("fetch", ["vless://resolved-node-1#Test1", "vless://resolved-node-2#Test2"]);
        var service = CreateService(connections, [fakeEngine]);

        // Act
        var base64Result = await service.GetSubscriptionAsync("client2");

        // Assert
        Assert.NotNull(base64Result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Result));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("vless://resolved-node-1#Test1", lines[0]);
        Assert.Equal("vless://resolved-node-2#Test2", lines[1]);
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

    private class FakeEngine(string engineName, List<string> resolvedLines) : ISubscriptionEngine
    {
        public bool CanHandle(string? engine) =>
            string.Equals(engine, engineName, StringComparison.OrdinalIgnoreCase);

        public Task<List<string>> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default) =>
            Task.FromResult(resolvedLines);
    }

    private class FakeUriEditor : IUriEditor
    {
        public string SetHostAndPort(string uri, string? host = null, int? port = null) => uri;
        public string SetQueryParameter(string uri, string key, string value) => uri;
        public string RemoveQueryParameter(string uri, string key) => uri;
    }
}
