using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class StaticConnectionEngineTests
{
    private readonly StaticConnectionEngine _engine = new(new FakeUriEditor());

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
    public async Task ResolveAsync_ShouldReturnFormattedLine_WhenTitleProvided()
    {
        var connection = new VpnConnectionDetails
        {
            ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp",
            Title = "My Node"
        };

        var result = await _engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#My Node", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUpdateHostAndPort_WhenSpecified()
    {
        var connection = new VpnConnectionDetails
        {
            ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp",
            Host = "custom.example.com",
            Port = 8443,
            Title = "Updated Node"
        };

        var result = await _engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("custom.example.com:8443", result);
        Assert.Contains("#Updated Node", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUpdatePortOnly_WhenHostNotSpecified()
    {
        var connection = new VpnConnectionDetails
        {
            ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp",
            Port = 9443
        };

        var result = await _engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("example.com:9443", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldPreserveExistingFragment_WhenConnectionStringContainsHash()
    {
        var connection = new VpnConnectionDetails
        {
            ConnectionString = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#Existing",
            Title = "Ignored"
        };

        var result = await _engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.Equal("vless://00000000-0000-0000-0000-000000000000@example.com:443?type=tcp#Existing", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_ShouldReturnNull_WhenConnectionStringIsNullOrWhiteSpace(string? connectionString)
    {
        var connection = new VpnConnectionDetails
        {
            ConnectionString = connectionString
        };

        var result = await _engine.ResolveAsync(connection, TestContext.Current.CancellationToken);

        Assert.Null(result);
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
