using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace ProperTea.Infrastructure.OpenApi;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var schemeId = "Bearer";

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes[schemeId] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste your Service Account JWT here."
            };
        }
    }
}
