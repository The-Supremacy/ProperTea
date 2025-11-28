using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ProperTea.Infrastructure.OpenTelemetry;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.Tenancy;
using ProperTea.Landlord.Bff.Endpoints;
using ProperTea.Landlord.Bff.Services;
using ProperTea.Landlord.Bff.Transforms;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Global Error Handling.
builder.AddGlobalErrorHandling(options =>
{
    options.ServiceName = "Landlord.Bff";
});

// OpenTelemetry
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() 
                  ?? new OpenTelemetryOptions();
builder.AddOpenTelemetry(otelOptions);
builder.AddProperHealthChecks();

// Infra.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProperTea_Landlord_Bff";
});

// YARP.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext => { builderContext.AddRequestTransform(OrganizationTransform.Transform); });

// Auth.
builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ProperTea.Landlord.Cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Oidc:SslRequired");
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("organization:*");
        options.Scope.Add("offline_access");

        options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
        {
            var idToken = await context.HttpContext.GetTokenAsync("id_token");
            if (idToken is not null)
            {
                context.ProtocolMessage.IdTokenHint = idToken;
            }
        };
    });

builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, ticketStore)
        =>
    {
        options.SessionStore = ticketStore;
    });
builder.Services.AddOpenIdConnectAccessTokenManagement();

builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("RequireAuthorization", options =>
        options.RequireAuthenticatedUser());

// Other services.
builder.Services.AddHealthChecks();
builder.Services.AddMultiTenancy();

var app = builder.Build();

app.UseGlobalErrorHandling();
app.UseRouting();
app.UseMultiTenancy();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.MapProperTelemetryEndpoints();
AuthEndpoints.Map(app);

app.MapGet("/", () => Results.Ok("Landlord.Bff"));

app.Run();