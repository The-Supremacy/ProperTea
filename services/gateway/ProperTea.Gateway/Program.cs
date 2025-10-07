using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Gateway.Middleware;
using ProperTea.Gateway.Services;
using ProperTea.ProperTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IAuthorizationService, AuthorizationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Authorization:BaseUrl"]!);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("External", options =>
    {
        options.Authority = builder.Configuration["Authentication:External:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Authentication:External:Issuer"],
            ValidAudience = builder.Configuration["Authentication:External:Audience"],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddSingleton<IInternalTokenService, InternalTokenService>();
builder.Services.AddTransient<IAuthorizationService, AuthorizationService>();

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

builder.AddProperTelemetry(builder.Configuration.GetRequiredSection("ProperTelemetry").Get<OpenTelemetryOptions>()!);
builder.AddProperHealthChecks();

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantValidationMiddleware>();
app.UseMiddleware<InternalTokenMiddleware>();

app.MapProperTelemetryEndpoints();

app.MapGet("/.well-known/jwks", (IInternalTokenService tokenService, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting JWKs");
    logger.LogError("Getting JWKs BIG ERROR");
    return Results.Json(tokenService.GetJwks());
});

app.MapReverseProxy();

app.Run();