using System.Text;
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

                var keyBytes = Encoding.UTF8.GetBytes(authOptions.SecretKey);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Authority,
                    ValidateAudience = !string.IsNullOrEmpty(authOptions.Audience),
                    ValidAudience = authOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Headers.TryGetValue("X-authentik-jwt", out var headerToken))
                        {
                            context.Token = headerToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
