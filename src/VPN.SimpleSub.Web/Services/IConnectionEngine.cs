using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public interface IConnectionEngine
{
    bool CanHandle(string? engine);
    Task<string?> ResolveAsync(VpnConnectionDetails connection, CancellationToken cancellationToken = default);
}
