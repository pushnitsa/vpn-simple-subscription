namespace VPN.SimpleSub.Web.Services;

public interface ISubscriptionService
{
    Task<string?> GetSubscriptionAsync(string clientId);
}