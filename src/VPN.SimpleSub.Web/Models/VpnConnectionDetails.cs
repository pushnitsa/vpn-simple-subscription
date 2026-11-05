namespace VPN.SimpleSub.Web.Models;

public class VpnConnectionDetails
{
    /// <summary>
    /// Engine type: "static" (default) or "fetch".
    /// </summary>
    public string Engine { get; init; } = "static";

    public string? Title { get; init; }

    // Static Engine
    public string? ConnectionString { get; init; }

    // Fetch Engine
    public string? SubscriptionUrl { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
}
