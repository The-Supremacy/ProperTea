using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using ProperTea.Infrastructure.Auth;

namespace ProperTea.Infrastructure.OpenApi;

public sealed class OAuth2SecuritySchemeTransformer(ProperAuthOptions authOptions)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemeId = "oauth2";

        var oauthScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Authentik OIDC Flow",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(authOptions.AuthorizationUrl),
                    TokenUrl = new Uri(authOptions.TokenUrl),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID" },
                        { "profile", "Profile" },
                        { "email", "Email" }
                    }
                }
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[schemeId] = oauthScheme;

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Id = schemeId, Type = ReferenceType.SecurityScheme }
            }] = new[] { "openid", "profile", "email" }
        };

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
            operation.Value.Security.Add(requirement);
        }

        return Task.CompletedTask;
    }
}
