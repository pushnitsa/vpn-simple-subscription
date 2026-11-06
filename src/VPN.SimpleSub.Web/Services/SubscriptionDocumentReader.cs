using System.Text.Json;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionDocumentReader : ISubscriptionDocumentReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SubscriptionsDocument?> ReadAsync(string discoveryPath, CancellationToken cancellationToken = default)
    {
        var fileContent = await File.ReadAllTextAsync(discoveryPath, cancellationToken);
        return JsonSerializer.Deserialize<SubscriptionsDocument>(fileContent, SerializerOptions);
    }
}
