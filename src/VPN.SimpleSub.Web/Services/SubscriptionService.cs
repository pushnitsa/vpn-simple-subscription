using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppSettings _appSettings;
    private readonly List<VpnConnection> _vpnConnections = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? _expiration;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public SubscriptionService(IOptions<AppSettings> options)
    {
        _appSettings = options.Value;
    }
    
    public async Task<string?> GetSubscriptionAsync(string clientId)
    {
        await LoadVpnConnectionsAsync();
        
        var result = _vpnConnections.FirstOrDefault(x => x.ClientId == clientId);
        
        if (result == null)
        {
            return null;
        }
        else
        {
            var joinedString = string.Join('\n', result.Connections.Select(x => $"{x.ConnectionString}#{x.Title}"));
            var base64EncodedBytes = Convert.ToBase64String(Encoding.UTF8.GetBytes(joinedString));
            return base64EncodedBytes;
        }
    }
    
    private async Task LoadVpnConnectionsAsync()
    {
        if (_expiration == null || _expiration < DateTime.UtcNow)
        {
            await _semaphoreSlim.WaitAsync();
            
            var fileContent = await File.ReadAllTextAsync(_appSettings.SubscriptionStorageDiscoveryPath);
            
            _vpnConnections.Clear();
            _vpnConnections.AddRange(JsonSerializer.Deserialize<List<VpnConnection>>(fileContent, _jsonSerializerOptions)!);
            
            _expiration = DateTime.UtcNow.AddMinutes(5);
            
            _semaphoreSlim.Release();
        }
    }
}