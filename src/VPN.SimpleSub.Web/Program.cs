using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IUriEditor, VlessConfigEditor>();
builder.Services.AddSingleton<ISubscriptionFetcher, SubscriptionFetcher>();
builder.Services.AddSingleton<ISubscriptionEngine, StaticSubscriptionEngine>();
builder.Services.AddSingleton<ISubscriptionEngine, FetchSubscriptionEngine>();
builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();

var app = builder.Build();

var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
app.MapControllerRoute(
    "subsController", 
    $"{appSettings.SubscriptionRoute}/{{clientId}}/{{action}}", 
    new  { controller = "Subscription" });

app.Run();
