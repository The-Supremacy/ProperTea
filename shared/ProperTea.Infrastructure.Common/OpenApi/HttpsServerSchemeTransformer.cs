using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProperTea.Infrastructure.Common.OpenApi;

public class HttpsServerSchemeTransformer() : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!document.Servers!.Any())
            return Task.CompletedTask;

        var url = document.Servers![0].Url!;
        document.Servers.Clear();

        document.Servers.Add(new OpenApiServer
        {
            Url = url.Replace("http://", "https://"),
            Description = "ProperTea Gateway"
        });

        return Task.CompletedTask;
    }
}
