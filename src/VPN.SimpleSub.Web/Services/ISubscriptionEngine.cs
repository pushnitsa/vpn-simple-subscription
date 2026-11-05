using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public interface ISubscriptionEngine
{
    bool CanHandle(string? engine);
    Task<List<string>> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default);
}
