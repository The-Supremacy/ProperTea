using Microsoft.AspNetCore.Authentication.JwtBearer;
using ProperTea.ServiceDefaults.Auth;
using Zitadel.Credentials;
using Zitadel.Extensions;

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

        var appJwtPath = configuration["Zitadel:AppJwtPath"]
            ?? throw new InvalidOperationException("Zitadel:AppJwtPath not configured");

        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddZitadelIntrospection(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.JwtProfile = Application.LoadFromJsonFile(appJwtPath);
            });

        _ = services.AddAuthorization();

        _ = services.AddTransient<IUserContext, UserContext>();

        return services;
    }
}
