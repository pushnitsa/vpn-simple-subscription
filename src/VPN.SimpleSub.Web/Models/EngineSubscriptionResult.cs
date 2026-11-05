namespace VPN.SimpleSub.Web.Models;

public record EngineSubscriptionResult(
    IReadOnlyList<string> Configs,
    IReadOnlyDictionary<string, string>? Headers = null
);
