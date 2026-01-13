using Zitadel.Credentials;
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

        var serviceAccountPath = configuration["Zitadel:ServiceAccountPath"]
            ?? throw new InvalidOperationException("Zitadel:ServiceAccountPath not configured");

        _ = services.AddAuthentication("Zitadel")
            .AddZitadelIntrospection("Zitadel", options =>
            {
                options.Authority = authority;
                options.JwtProfile = Application.LoadFromJsonFile(serviceAccountPath);
            });

        _ = services.AddAuthorization();

        return services;
    }
}
