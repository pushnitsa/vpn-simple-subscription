using System.Text.Json.Serialization;

namespace VPN.SimpleSub.Web.Models;

public class SubscriptionsDocument
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    public List<VpnConnection> Subscriptions { get; init; } = [];
}
