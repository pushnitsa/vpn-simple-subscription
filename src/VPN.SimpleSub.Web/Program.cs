using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();

var app = builder.Build();

var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
app.MapControllerRoute(
    "subsController", 
    $"{appSettings.SubscriptionRoute}/{{clientId}}/{{action}}", 
    new  { controller = "Subscription" });

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            context.Response.Headers.Append("Profile-Update-Interval", "12");
        }
        
        return Task.FromResult(0);
    });
    
    await next();
});

app.Run();
