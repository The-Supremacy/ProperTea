using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ProperTea.Company.Config;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var oidcAuthority = configuration["OIDC:Authority"]
            ?? throw new InvalidOperationException("OIDC:Authority configuration is missing");
        var oidcIssuer = configuration["OIDC:Issuer"]
            ?? throw new InvalidOperationException("OIDC:Issuer configuration is missing");
        var oidcAudience = configuration["OIDC:Audience"]
            ?? throw new InvalidOperationException("OIDC:Audience configuration is missing");

        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = oidcAuthority;
                options.Audience = oidcAudience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = oidcIssuer,
                    ValidateAudience = true,
                    ValidAudience = oidcAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        _ = services.AddAuthorization();

        return services;
    }
}
