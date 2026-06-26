using Microsoft.AspNetCore.Mvc;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Web.Controllers;

public class SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
    : ControllerBase
{
    [ActionName("plain")]
    public async Task<IActionResult> Index(string clientId)
    {
        var result = await subscriptionService.GetSubscriptionAsync(clientId);
        
        if (string.IsNullOrEmpty(result))
        {
            logger.LogDebug("Subscription not found");
            return NotFound();
        }
        else
        {
            return Ok(result);
        }
    }
}