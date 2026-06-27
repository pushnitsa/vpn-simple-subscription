namespace VPN.SimpleSub.Web.Models;

public class VpnConnectionOld
{
    public required string ClientId { get; init; }
    
    public required List<string> Connections { get; init; }
}