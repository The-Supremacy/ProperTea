using ProperTea.Infrastructure.Common.OpenApi;
using Scalar.AspNetCore;

namespace ProperTea.Company.Config;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.AddOpenApi(options =>
        {
            _ = options.AddDocumentTransformer<HttpsServerSchemeTransformer>();
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
            _ = app.MapScalarApiReference();
        }

        return app;
    }
}
