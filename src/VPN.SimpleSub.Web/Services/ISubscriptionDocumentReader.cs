using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public interface ISubscriptionDocumentReader
{
    Task<SubscriptionsDocument?> ReadAsync(string discoveryPath, CancellationToken cancellationToken = default);
}
