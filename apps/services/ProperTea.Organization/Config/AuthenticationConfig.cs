
using Zitadel.Extensions;

namespace ProperTea.Organization.Config;

public static class AuthenticationConfig
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["OIDC:Authority"]
            ?? throw new InvalidOperationException("OIDC:Authority not configured");

        var clientId = configuration["OIDC:ClientId"]
            ?? throw new InvalidOperationException("OIDC:ClientId not configured");

        var clientSecret = configuration["OIDC:ClientSecret"]
            ?? throw new InvalidOperationException("OIDC:ClientSecret not configured");

        _ = services.AddAuthentication("Zitadel")
            .AddZitadelIntrospection("Zitadel", options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            });

        _ = services.AddAuthorization();

        return services;
    }
}
