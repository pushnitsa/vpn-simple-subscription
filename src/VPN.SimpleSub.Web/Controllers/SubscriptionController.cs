using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Web.Controllers;

public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;
    
    public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(string clientId)
    {
        var result = await _subscriptionService.GetSubscriptionAsync(clientId);
        
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogDebug("Subscription not found");
            return NotFound();
        }
        else
        {
            return Ok(result);
        }
    }
}