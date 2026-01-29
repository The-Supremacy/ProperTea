using Microsoft.AspNetCore.Authentication.JwtBearer;
using ProperTea.Infrastructure.Common.Auth;
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
        var issuer = configuration["OIDC:Issuer"]
            ?? throw new InvalidOperationException("OIDC:Issuer not configured");
        var audience = configuration["OIDC:Audience"]
            ?? throw new InvalidOperationException("OIDC:Audience not configured");

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
        _ = services.AddTransient<IOrganizationIdProvider, TenantIdProvider>();

        return services;
    }
}
