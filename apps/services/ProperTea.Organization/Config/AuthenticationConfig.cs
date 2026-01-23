using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProperTea.ServiceDefaults.Auth;

namespace ProperTea.Organization.Config;

public static class AuthenticationConfig
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var oidcSection = configuration.GetSection("OIDC");
                var authority = oidcSection["Authority"]
                    ?? throw new InvalidOperationException("OIDC:Authority not configured");
                var issuer = oidcSection["Issuer"]
                    ?? throw new InvalidOperationException("OIDC:Issuer not configured");

                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = false,

                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(5)
                };
                options.RequireHttpsMetadata = environment.IsProduction();
            });

        _ = services.AddAuthorization();

        _ = services.AddTransient<IUserContext, UserContext>();

        return services;
    }
}
