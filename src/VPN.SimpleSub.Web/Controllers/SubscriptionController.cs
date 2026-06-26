using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Web.Controllers;

public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    
    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }
    
    public async Task<IActionResult> Index(string clientId)
    {
        var result = await _subscriptionService.GetSubscriptionAsync(clientId);
        
        if (string.IsNullOrEmpty(result))
        {
            return NotFound();
        }
        else
        {
            return Ok(result);
        }
    }
}