using Microsoft.Extensions.DependencyInjection;

namespace ProperTea.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddProperOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1", options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); });
        return services;
    }
}
