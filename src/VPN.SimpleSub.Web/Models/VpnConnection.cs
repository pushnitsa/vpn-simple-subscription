namespace VPN.SimpleSub.Web.Models;

public class VpnConnection
{
    public required string ClientId { get; init; }

    /// <summary>
    /// Engine type: "static" (default) or "fetch".
    /// </summary>
    public string Engine { get; init; } = "static";

    // Static Engine
    public List<VpnConnectionDetails>? Connections { get; init; }

    // Fetch Engine
    public string? SubscriptionUrl { get; init; }
    public List<VpnConnectionOverride>? Overrides { get; init; }

    public string? Notes { get; init; }
}
