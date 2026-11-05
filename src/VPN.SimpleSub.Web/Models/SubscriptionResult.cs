namespace VPN.SimpleSub.Web.Models;

public record SubscriptionResult(
    IReadOnlyList<string> VlessConfigs,
    string? ProfileTitle = null,
    int? ProfileUpdateInterval = null,
    string? SubscriptionUserInfo = null
)
{
    public SubscriptionResult(
        string vlessConfig,
        string? profileTitle = null,
        int? profileUpdateInterval = null,
        string? subscriptionUserInfo = null)
        : this(new[] { vlessConfig }, profileTitle, profileUpdateInterval, subscriptionUserInfo)
    {
    }

    public string VlessConfig => VlessConfigs.Count > 0 ? VlessConfigs[0] : string.Empty;
}
