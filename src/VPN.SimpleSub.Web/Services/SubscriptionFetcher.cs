using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionFetcher(HttpClient? httpClient = null) : ISubscriptionFetcher
{
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

    public Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
