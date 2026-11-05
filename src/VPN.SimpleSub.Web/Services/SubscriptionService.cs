using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionService(
    IOptions<AppSettings> options,
    IEnumerable<IConnectionEngine> engines) : ISubscriptionService
{
    private readonly List<VpnConnection> _vpnConnections = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? _expiration;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string?> GetSubscriptionAsync(string clientId)
    {
        await LoadVpnConnectionsAsync();

        var result = _vpnConnections.FirstOrDefault(x => x.ClientId == clientId);
        if (result == null)
        {
            return null;
        }

        var lines = new List<string>();

        foreach (var connection in result.Connections)
        {
            var engine = engines.FirstOrDefault(e => e.CanHandle(connection.Engine));
            if (engine != null)
            {
                var line = await engine.ResolveAsync(connection);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }
        }

        if (lines.Count == 0)
        {
            return null;
        }

        var joinedString = string.Join('\n', lines);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(joinedString));
    }

    private async Task LoadVpnConnectionsAsync()
    {
        if (_expiration == null || _expiration < DateTime.UtcNow)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                var fileContent = await File.ReadAllTextAsync(options.Value.SubscriptionStorageDiscoveryPath);

                _vpnConnections.Clear();
                _vpnConnections.AddRange(
                    JsonSerializer.Deserialize<List<VpnConnection>>(fileContent, _jsonSerializerOptions)!);

                _expiration = DateTime.UtcNow.AddMinutes(5);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
