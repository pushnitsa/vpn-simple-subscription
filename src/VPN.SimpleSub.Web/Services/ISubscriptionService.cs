using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponse?> GetSubscriptionAsync(string clientId);
}
