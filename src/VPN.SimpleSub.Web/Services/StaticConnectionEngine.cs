using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class StaticConnectionEngine : IConnectionEngine
{
    public bool CanHandle(string? engine) =>
        string.IsNullOrEmpty(engine) || string.Equals(engine, "static", StringComparison.OrdinalIgnoreCase);

    public Task<string?> ResolveAsync(VpnConnectionDetails connection, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.ConnectionString))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(ConnectionLineFormatter.Format(connection.ConnectionString, connection.Title));
    }
}
