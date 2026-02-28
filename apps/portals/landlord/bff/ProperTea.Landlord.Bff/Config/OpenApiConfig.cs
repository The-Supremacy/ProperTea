using ProperTea.Infrastructure.Common.OpenApi;
using Scalar.AspNetCore;

namespace ProperTea.Landlord.Bff.Config;

public static class OpenApiConfig
{
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
        _ = services.AddOpenApi(options =>
        {
            _ = options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
        });

        return services;
    }

    public static WebApplication UseOpenApi(
        this WebApplication app,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            _ = app.MapOpenApi();

            var clientId = configuration["Scalar:ClientId"]
                ?? throw new InvalidOperationException("Scalar:ClientId not configured");

            _ = app.MapScalarApiReference(options =>
            {
                _ = options
                    .WithTitle("ProperTea Landlord BFF API")
                    .AddPreferredSecuritySchemes("oauth2")
                    .AddAuthorizationCodeFlow("oauth2", flow =>
                    {
                        flow.ClientId = clientId;
                        flow.Pkce = Pkce.Sha256;
                        flow.SelectedScopes = [
                            "openid",
                            "profile",
                            "email",
                            "offline_access",
                            "organization"
                        ];
                    });
            });
        }

        return app;
    }
}
