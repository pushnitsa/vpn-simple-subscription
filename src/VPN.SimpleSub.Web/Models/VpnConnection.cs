namespace VPN.SimpleSub.Web.Models;

public class VpnConnection
{
    public required string ClientId { get; init; }
    
    public required List<string> Connections { get; init; }
}