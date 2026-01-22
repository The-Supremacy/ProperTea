using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Auth;

namespace ProperTea.Organization.Config;

public static class AuthenticationConfig
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var oidcSection = configuration.GetSection("OIDC");
                var authority = oidcSection["Authority"]
                    ?? throw new InvalidOperationException("OIDC:Authority not configured");
                var issuer = oidcSection["Issuer"]
                    ?? throw new InvalidOperationException("OIDC:Issuer not configured");

                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = false,

                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(5)
                };
                options.RequireHttpsMetadata = environment.IsProduction();
            });

        _ = services.AddAuthorization();

        _ = services.AddTransient<IExternalOrganizationClient, KeycloakOrganizationClient>();
        _ = services.AddTransient<IUserContext, UserContext>();

        // Need this wireup to call Keycloak Admin API
        var tokenClientName = "keycloak_admin_client";
        var authServerUrl = configuration["Keycloak:AuthServerUrl"] ?? throw new InvalidOperationException("Missing Keycloak:AuthServerUrl");
        var realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Missing Keycloak:Realm");
        var clientId = configuration["Keycloak:Resource"] ?? throw new InvalidOperationException("Missing Keycloak:Resource");
        var clientSecret = configuration["Keycloak:Credentials:Secret"] ?? throw new InvalidOperationException("Missing Keycloak:Credentials:Secret");
        var tokenEndpoint = authServerUrl + $"realms/{realm}/protocol/openid-connect/token";
        _ = services.AddClientCredentialsTokenManagement()
            .AddClient(tokenClientName, client =>
            {
                client.TokenEndpoint = new Uri(tokenEndpoint);
                client.ClientId = ClientId.Parse(clientId);
                client.ClientSecret = ClientSecret.Parse(clientSecret);
            });

        _ = services.AddKeycloakAdminHttpClient(configuration)
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse(tokenClientName));

        return services;
    }
}
