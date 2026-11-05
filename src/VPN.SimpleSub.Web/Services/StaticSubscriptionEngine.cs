using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class StaticSubscriptionEngine(IUriEditor uriEditor) : ISubscriptionEngine
{
    public bool CanHandle(string? engine) =>
        string.IsNullOrEmpty(engine) || string.Equals(engine, "static", StringComparison.OrdinalIgnoreCase);

    public Task<List<string>> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default)
    {
        var result = new List<string>();

        if (connection.Connections == null)
        {
            return Task.FromResult(result);
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

        return Task.FromResult(result);
    }
}
