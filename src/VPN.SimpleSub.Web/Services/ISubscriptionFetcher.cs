using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public interface ISubscriptionFetcher
{
    Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default);
}
