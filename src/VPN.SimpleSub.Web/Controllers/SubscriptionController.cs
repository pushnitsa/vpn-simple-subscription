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
        
        if (result == null || string.IsNullOrEmpty(result.Content))
        {
            logger.LogDebug("Subscription not found");
            return NotFound();
        }

        foreach (var (key, value) in result.Headers)
        {
            Response.Headers[key] = value;
        }

        return Ok(result.Content);
    }
}
