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
                    AuthorizationUrl = new Uri($"{url}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{url}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "User profile" },
                        { "email", "Email address" },
                        { "offline_access", "Offline access" },
                        { "organization", "Organization membership" }
                    }
                }
            }
        });

        document.Security = [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("oauth2"),
                    ["openid", "profile", "email", "organization"]
                }
            }
        ];

        document.SetReferenceHostDocument();

        return Task.CompletedTask;
    }
}
