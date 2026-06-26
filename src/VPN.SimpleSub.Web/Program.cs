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
    $"{appSettings.SubscriptionRoute}/{{clientId?}}", 
    new  { controller = "Subscription", action = "Index" });

app.Run();
