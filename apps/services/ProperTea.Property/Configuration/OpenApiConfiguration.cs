using ProperTea.Infrastructure.Common.OpenApi;
using Scalar.AspNetCore;

namespace ProperTea.Property.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
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
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            _ = app.MapOpenApi();

            var clientId = configuration["Scalar:ClientId"]
                ?? throw new InvalidOperationException("Scalar:ClientId not configured");
            _ = app.MapScalarApiReference(options =>
            {
                _ = options
                    .WithTitle("ProperTea Property API")
                    .AddPreferredSecuritySchemes("oauth2")
                    .AddAuthorizationCodeFlow("oauth2", flow =>
                    {
                        flow.ClientId = clientId;
                        flow.Pkce = Pkce.Sha256;
                        flow.SelectedScopes = [
                            "openid",
                            "profile",
                            "email",
                            "aud",
                            "offline_access",
                            "urn:zitadel:iam:user:resourceowner",
                            "urn:zitadel:iam:org:project:id:zitadel:aud"
                        ];
                    });
            });
        }

        return app;
    }
}
