using IdentityModel.AspNetCore.OAuth2Introspection;
using ProperTea.Infrastructure.Common.Auth;

namespace ProperTea.Organization.Config;

public static class AuthenticationConfig
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authority = configuration["OIDC:Authority"]
            ?? throw new InvalidOperationException("OIDC:Authority not configured");

        _ = services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
            .AddOAuth2Introspection(options =>
            {
                options.Authority = authority;
                options.ClientId = configuration["Keycloak:Resource"]
                    ?? throw new InvalidOperationException("Keycloak:Resource not configured");
                options.ClientSecret = configuration["Keycloak:Credentials:Secret"]
                    ?? throw new InvalidOperationException("Keycloak:Credentials:Secret not configured");
            });

        _ = services.AddAuthorization();

        _ = services.AddTransient<IUserContext, UserContext>();
        _ = services.AddTransient<IOrganizationIdProvider, OrganizationIdProvider>();

        return services;
    }
}
