using System.Text;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionService(
    IOptions<AppSettings> options,
    IEnumerable<ISubscriptionEngine> engines,
    ISubscriptionDocumentReader documentReader) : ISubscriptionService
{
    private readonly List<VpnConnection> _vpnConnections = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? _expiration;

    public async Task<SubscriptionResponse?> GetSubscriptionAsync(string clientId)
    {
        await LoadVpnConnectionsAsync();

        var result = _vpnConnections.FirstOrDefault(x => x.ClientId == clientId);
        if (result == null)
        {
            return null;
        }

        var engine = engines.FirstOrDefault(e => e.CanHandle(result.Engine));
        if (engine == null)
        {
            return null;
        }

        var engineResult = await engine.ResolveConfigsAsync(result);
        if (engineResult.Configs.Count == 0)
        {
            return null;
        }

        var joinedString = string.Join('\n', engineResult.Configs);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(joinedString));
        var headers = engineResult.Headers ?? new Dictionary<string, string>();

        return new SubscriptionResponse(base64, headers);
    }

    private async Task LoadVpnConnectionsAsync()
    {
        if (_expiration == null || _expiration < DateTime.UtcNow)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                var doc = await documentReader.ReadAsync(options.Value.SubscriptionStorageDiscoveryPath);

                _vpnConnections.Clear();
                if (doc?.Subscriptions != null)
                {
                    _vpnConnections.AddRange(doc.Subscriptions);
                }

                _expiration = DateTime.UtcNow.AddMinutes(5);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
