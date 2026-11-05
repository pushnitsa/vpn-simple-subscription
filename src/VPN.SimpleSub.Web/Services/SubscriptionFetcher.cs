using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionFetcher(IHttpClientFactory httpClientFactory) : ISubscriptionFetcher
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
