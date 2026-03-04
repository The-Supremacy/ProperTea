using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.Kiota.Abstractions;

namespace ProperTea.User.Features.UserProfiles.Infrastructure;

public class KeycloakUserClient(
    KeycloakAdminApiClient adminApiClient,
    IConfiguration configuration,
    ILogger<KeycloakUserClient> logger) : IExternalUserClient
{
    private readonly string _realm =
        configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak:Realm not configured");

    public async Task<ExternalUserDetails?> GetUserDetailsAsync(
        string externalUserId,
        CancellationToken ct = default)
    {
        try
        {
            var user = await adminApiClient.Admin.Realms[_realm].Users[externalUserId]
                .GetAsync(cancellationToken: ct);

            if (user is null) return null;

            logger.LogDebug("Retrieved user from Keycloak: {Email} ({Id})", user.Email, user.Id);

            return new ExternalUserDetails(
                user.Id ?? externalUserId,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            logger.LogWarning("User not found in Keycloak: {Id}", externalUserId);
            return null;
        }
    }
}

