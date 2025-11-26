using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ProperTea.Core.Tenancy;
using ProperTea.Infrastructure.Tenancy;
using ProperTea.Landlord.Bff.Middleware;
using ProperTea.Landlord.Bff.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProperTea_Landlord_Bff";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<TokenRefreshService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ProperTea.Landlord.Cookie";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents()
        {
            OnValidatePrincipal = async (context) =>
            {
                var refreshService = context.HttpContext.RequestServices.GetRequiredService<TokenRefreshService>();
                await refreshService.RefreshTokenAsync(context);
            }
        };
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("organization:*");
        options.Scope.Add("offline_access");

        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Oidc:SslRequired");

        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "realm_access";
    });

builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("RequireAuthorization", options =>
        options.RequireAuthenticatedUser());

builder.Services.AddHealthChecks();
builder.Services.AddMultiTenancy();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OrganizationContextMiddleware>();
app.UseMultiTenancy();

app.MapGet("/", () => "Hello from the Landlord BFF Gateway!");
app.MapHealthChecks("/health");

app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }))
    .WithName("Login");

app.MapGet("/logout", () => Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]
    ))
    .WithName("Logout");

app.MapGet("/user", (ClaimsPrincipal user) => Results.Ok(user.Claims.Select(c => new { c.Type, c.Value })))
    .RequireAuthorization();

app.MapGet("/api/{orgId}/tenant", (string orgId, ICurrentOrganizationProvider provider) =>
    {
        return Results.Ok(new {
            OrganizationFromUrl = orgId,
            OrganizationIdFromProvider = provider.OrganizationId
        });
    })
    .RequireAuthorization();

app.MapReverseProxy();

app.Run();