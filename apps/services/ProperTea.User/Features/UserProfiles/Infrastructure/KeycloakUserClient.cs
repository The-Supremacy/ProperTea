using System.Net.Http.Json;
using System.Text.Json;

namespace ProperTea.User.Features.UserProfiles.Infrastructure;

public class KeycloakUserClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<KeycloakUserClient> logger) : IExternalUserClient
{
    private readonly string _realmBase =
        $"admin/realms/{configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak:Realm not configured")}";

    public async Task<ExternalUserDetails?> GetUserDetailsAsync(
        string externalUserId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"{_realmBase}/users/{externalUserId}", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("User not found in Keycloak: {Id}", externalUserId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var root = document.RootElement;
        var id = root.GetProperty("id").GetString() ?? externalUserId;
        var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? string.Empty : string.Empty;
        var firstName = root.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() : null;
        var lastName = root.TryGetProperty("lastName", out var lnProp) ? lnProp.GetString() : null;

        logger.LogDebug("Retrieved user from Keycloak: {Email} ({Id})", email, id);

        return new ExternalUserDetails(id, email, firstName, lastName);
    }
}
