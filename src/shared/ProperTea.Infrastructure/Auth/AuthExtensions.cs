using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ProperTea.Infrastructure.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddProperAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = new ProperAuthOptions();
        configuration.GetSection(ProperAuthOptions.SectionName).Bind(authOptions);
        services.AddSingleton(authOptions);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MetadataAddress = authOptions.InternalMetadataAddress;
                options.RequireHttpsMetadata = authOptions.RequireHttps;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = authOptions.Authority,

                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,

                    ValidateLifetime = true
                };
            });

        services.AddAuthorization();
        return services;
    }
}
