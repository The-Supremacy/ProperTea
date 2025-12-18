using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ProperTea.Landlord.Bff.Auth;

namespace ProperTea.Landlord.Bff.Config
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddBffAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            _ = services.AddTransient<ITicketStore, RedisTicketStore>();

            _ = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "landlord-session";
                options.Cookie.SameSite = SameSiteMode.Strict; // BFF and FE are on the same host
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                var ticketStore = services.BuildServiceProvider().GetRequiredService<ITicketStore>();
                options.SessionStore = ticketStore;

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/bff"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var oidc = configuration.GetSection("OIDC");

                options.Authority = oidc["Authority"];
                options.ClientId = oidc["ClientId"];
                options.ClientSecret = oidc["ClientSecret"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false; // Use standard OIDC claims

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");

                // ZITADEL specific scopes for orgs/roles
                options.Scope.Add("urn:zitadel:iam:user:resourceowner");
                options.Scope.Add("urn:zitadel:iam:org:project:roles");
                options.Scope.Add("urn:zitadel:iam:org:project:id:zitadel:aud");

                // Standard OIDC paths
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
            });

            _ = services.AddAuthorization();

            return services;
        }
    }
}
