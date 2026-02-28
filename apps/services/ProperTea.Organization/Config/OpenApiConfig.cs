using ProperTea.Infrastructure.Common.OpenApi;
using Scalar.AspNetCore;

namespace ProperTea.Organization.Config;

public static class OpenApiConfig
{
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services, IConfiguration configuration)
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
                ?? throw new InvalidOperationException("OIDC:ClientId not configured");
            _ = app.MapScalarApiReference(options =>
            {
                _ = options
                    .WithTitle("ProperTea Organization API")
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
