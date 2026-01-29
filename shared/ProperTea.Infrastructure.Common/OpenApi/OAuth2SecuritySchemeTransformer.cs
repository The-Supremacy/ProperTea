using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace ProperTea.Infrastructure.Common.OpenApi;

public sealed class OAuth2SecuritySchemeTransformer(IConfiguration configuration)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var url = configuration["OIDC:Authority"]
            ?? throw new InvalidOperationException("OIDC:Authority not configured");

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{url}/oauth/v2/authorize"),
                    TokenUrl = new Uri($"{url}/oauth/v2/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "User profile" },
                        { "email", "Email address" },
                        { "aud", "Audience" },
                        { "offline_access", "Offline access" },
                        { "urn:zitadel:iam:org:project:id:zitadel:aud", "ZITADEL audience" }
                    }
                }
            }
        });

        document.Security = [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("oauth2"),
                    ["api", "profile", "email", "openid"]
                }
            }
        ];

        document.SetReferenceHostDocument();

        return Task.CompletedTask;
    }
}
