using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class StaticConnectionEngine(IUriEditor uriEditor) : IConnectionEngine
{
    public bool CanHandle(string? engine) =>
        string.IsNullOrEmpty(engine) || string.Equals(engine, "static", StringComparison.OrdinalIgnoreCase);

    public Task<string?> ResolveAsync(VpnConnectionDetails connection, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.ConnectionString))
        {
            return Task.FromResult<string?>(null);
        }

        var uri = connection.ConnectionString;

        if (!string.IsNullOrWhiteSpace(connection.Host) || connection.Port.HasValue)
        {
            uri = uriEditor.SetHostAndPort(uri, connection.Host, connection.Port);
        }

        return Task.FromResult<string?>(ConnectionLineFormatter.Format(uri, connection.Title));
    }
}
