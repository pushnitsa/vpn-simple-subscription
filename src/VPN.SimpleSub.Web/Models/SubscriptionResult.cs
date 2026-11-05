namespace VPN.SimpleSub.Web.Models;

public record SubscriptionResult(
    string VlessConfig,
    string? ProfileTitle = null,
    int? ProfileUpdateInterval = null,
    string? SubscriptionUserInfo = null
);
