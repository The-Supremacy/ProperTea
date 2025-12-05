using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace ProperTea.Infrastructure.OpenApi;

public class HttpsServerSchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!document.Servers.Any())
            return Task.CompletedTask;

        var servers = document.Servers.ToList();
        document.Servers.Clear();

        foreach (var server in servers)
        {
            document.Servers.Add(new OpenApiServer
            {
                Url = server.Url.Replace("http://", "https://"),
                Description = server.Description,
                Extensions = server.Extensions,
                Variables = server.Variables
            });
        }

        return Task.CompletedTask;
    }
}
