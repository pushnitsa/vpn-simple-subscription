using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class StaticSubscriptionEngine(IUriEditor uriEditor) : ISubscriptionEngine
{
    public bool CanHandle(string? engine) =>
        string.IsNullOrEmpty(engine) || string.Equals(engine, "static", StringComparison.OrdinalIgnoreCase);

    public Task<EngineSubscriptionResult> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default)
    {
        var result = new List<string>();

        if (connection.Connections == null)
        {
            return Task.FromResult(new EngineSubscriptionResult(result));
        }

        foreach (var conn in connection.Connections)
        {
            if (string.IsNullOrWhiteSpace(conn.ConnectionString))
            {
                continue;
            }

            var uri = conn.ConnectionString;

            if (!string.IsNullOrWhiteSpace(conn.Host) || conn.Port.HasValue)
            {
                uri = uriEditor.SetHostAndPort(uri, conn.Host, conn.Port);
            }

            result.Add(ConnectionLineFormatter.Format(uri, conn.Title));
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SubscriptionHeaders.UpdateInterval] = SubscriptionHeaders.DefaultUpdateInterval
        };

        return Task.FromResult(new EngineSubscriptionResult(result, headers));
    }
}
