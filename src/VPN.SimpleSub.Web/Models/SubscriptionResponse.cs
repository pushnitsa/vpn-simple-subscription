namespace VPN.SimpleSub.Web.Models;

public record SubscriptionResponse(
    string Content,
    IReadOnlyDictionary<string, string> Headers
);
