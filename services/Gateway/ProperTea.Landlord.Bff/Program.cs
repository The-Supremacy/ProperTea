using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using ProperTea.Landlord.Bff.Endpoints.Auth;
using ProperTea.Landlord.Bff.Middleware;
using ProperTea.Landlord.Bff.Services;
using TheSupremacy.ProperErrorHandling;
using TheSupremacy.ProperTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("default", limiterOptions =>
    {
        limiterOptions.TokenLimit = 100;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        limiterOptions.TokensPerPeriod = 100;
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProperTea_Landlord_Bff_";
});

builder.AddProperGlobalErrorHandling("ProperTea.Landlord.Bff");
builder.AddProperTelemetry(builder.Configuration.GetRequiredSection("ProperTelemetry").Get<OpenTelemetryOptions>()!);
builder.AddProperHealthChecks();

builder.Services.AddHttpClient<IIdentityService, IdentityService>(client =>
{
    var baseAddress = builder.Configuration.GetValue<string>("ServiceEndpoints:Identity");
    if (string.IsNullOrEmpty(baseAddress))
        throw new InvalidOperationException("Identity service endpoint is not configured.");
    client.BaseAddress = new Uri(baseAddress);
});

var app = builder.Build();

app.UseProperGlobalErrorHandling();
app.MapProperTelemetryEndpoints();

app.UseRateLimiter();
app.MapReverseProxy();

app.UseMiddleware<SessionManagementMiddleware>();

app.MapAuthEndpoints();

app.Run();

// This is needed so that test can access the app.
namespace ProperTea.Landlord.Bff
{
    public class Program
    {
    }
}