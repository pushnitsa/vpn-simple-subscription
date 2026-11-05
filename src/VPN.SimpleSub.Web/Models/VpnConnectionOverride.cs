namespace VPN.SimpleSub.Web.Models;

public class VpnConnectionOverride
{
    public string? MatchSni { get; init; }
    public string? MatchHost { get; init; }

    public string? Title { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
}
