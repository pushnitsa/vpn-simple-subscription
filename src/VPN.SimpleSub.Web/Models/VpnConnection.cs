namespace VPN.SimpleSub.Web.Models;

public class VpnConnection
{
    public required string ClientId { get; init; }
    
    public required List<VpnConnectionDetails> Connections { get; init; }
    
    public string? Notes { get; init; }
}