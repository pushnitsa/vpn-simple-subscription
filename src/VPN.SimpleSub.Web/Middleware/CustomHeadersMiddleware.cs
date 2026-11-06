namespace VPN.SimpleSub.Web.Middleware;

public class CustomHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                context.Response.Headers.Append("Profile-Update-Interval", "12");
            }
            
            return Task.FromResult(0);
        });
        
        await next(context);
    }
}