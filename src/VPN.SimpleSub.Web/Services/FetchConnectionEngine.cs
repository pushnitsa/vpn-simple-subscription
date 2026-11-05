using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class FetchConnectionEngine(
    ISubscriptionFetcher subscriptionFetcher,
    IUriEditor uriEditor) : IConnectionEngine
{
    public bool CanHandle(string? engine) =>
        string.Equals(engine, "fetch", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> ResolveAsync(VpnConnectionDetails connection, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.SubscriptionUrl))
        {
            return null;
        }

        var fetchResult = await subscriptionFetcher.FetchAsync(connection.SubscriptionUrl, cancellationToken);
        var vlessUri = fetchResult.VlessConfig;

        if (!string.IsNullOrWhiteSpace(connection.Host) || connection.Port.HasValue)
        {
            vlessUri = uriEditor.SetHostAndPort(vlessUri, connection.Host, connection.Port);
        }

        var title = connection.Title ?? fetchResult.ProfileTitle;
        return ConnectionLineFormatter.Format(vlessUri, title);
    }
}
