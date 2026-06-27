using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ILogger<SubscriptionService> _logger;
    private readonly AppSettings _appSettings;
    private readonly List<VpnConnectionOld> _vpnConnections = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? _expiration;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public SubscriptionService(IOptions<AppSettings> options, ILogger<SubscriptionService> logger)
    {
        _logger = logger;
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
            var joinedString = string.Join('\n', result.Connections);
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
            
            var connections = Enumerable.Empty<VpnConnectionOld>();

            try
            {
                var deserializedConnections =
                    JsonSerializer.Deserialize<List<VpnConnectionNew>>(fileContent, _jsonSerializerOptions)!;
                
                connections = deserializedConnections.Select(x => new VpnConnectionOld
                {
                    ClientId = x.ClientId,
                    Connections = x.Connections.Select(c => $"{c.ConnectionString}#{c.Title}").ToList(),
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load VPN connections");
                connections = JsonSerializer.Deserialize<List<VpnConnectionOld>>(fileContent, _jsonSerializerOptions);
            }
            
            _vpnConnections.Clear();
            _vpnConnections.AddRange(connections!);
            
            _expiration = DateTime.UtcNow.AddMinutes(5);
            
            _semaphoreSlim.Release();
        }
    }
}