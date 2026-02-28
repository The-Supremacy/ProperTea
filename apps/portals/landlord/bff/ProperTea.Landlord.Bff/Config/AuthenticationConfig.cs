using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using ProperTea.Landlord.Bff.Auth;

namespace ProperTea.Landlord.Bff.Config
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddBffAuthentication(this IServiceCollection services, IConfiguration configuration, bool isDev)
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
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                var ticketStore = services.BuildServiceProvider().GetRequiredService<ITicketStore>();
                options.SessionStore = ticketStore;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var oidcSection = configuration.GetSection("OIDC");

                options.Authority = oidcSection["Authority"] ?? throw new InvalidOperationException("OIDC:Authority not configured");
                options.ClientId = oidcSection["ClientId"] ?? throw new InvalidOperationException("OIDC:ClientId not configured");
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UsePkce = true;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");

                options.Scope.Add("organization");

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";

                options.CallbackPath = "/auth/signin-oidc";
                options.SignedOutCallbackPath = "/auth/signout-callback-oidc";

                options.RequireHttpsMetadata = !isDev;

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (context.Properties.Items.TryGetValue("prompt", out var promptValue))
                        {
                            context.ProtocolMessage.Prompt = promptValue;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            _ = services.AddOpenIdConnectAccessTokenManagement(options =>
            {
                options.RefreshBeforeExpiration = TimeSpan.FromMinutes(5);
            });

            _ = services.AddAuthorization();

            return services;
        }
    }
}
