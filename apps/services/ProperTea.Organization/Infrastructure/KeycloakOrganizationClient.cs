using System.Net.Http.Json;
using System.Text.Json;

namespace ProperTea.Organization.Infrastructure;

public class KeycloakOrganizationClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<KeycloakOrganizationClient> logger) : IExternalOrganizationClient
{
    private readonly string _realmBase =
        $"admin/realms/{configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak:Realm not configured")}";

    public async Task<string> CreateOrganizationWithAdminAsync(
        string orgName,
        string email,
        string firstName,
        string lastName,
        string password,
        CancellationToken ct = default)
    {
        // 1. Create the Keycloak organization.
        var orgPayload = new { name = orgName, alias = orgName.ToLowerInvariant().Replace(" ", "-"), enabled = true };
        var orgResponse = await httpClient.PostAsJsonAsync($"{_realmBase}/organizations", orgPayload, ct);
        orgResponse.EnsureSuccessStatusCode();
        var orgId = ExtractIdFromLocation(orgResponse, "organization");

        logger.LogInformation("Created Keycloak organization {Name} with ID {OrgId}", orgName, orgId);

        // 2. Create the admin user.
        var userPayload = new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        };
        var userResponse = await httpClient.PostAsJsonAsync($"{_realmBase}/users", userPayload, ct);
        userResponse.EnsureSuccessStatusCode();
        var userId = ExtractIdFromLocation(userResponse, "user");

        logger.LogInformation("Created Keycloak user {Email} with ID {UserId}", email, userId);

        // 3. Add the user to the organization.
        // Keycloak expects the user ID as a JSON string body.
        var memberResponse = await httpClient.PostAsJsonAsync(
            $"{_realmBase}/organizations/{orgId}/members",
            userId,
            ct);
        memberResponse.EnsureSuccessStatusCode();

        logger.LogInformation("Added user {UserId} to organization {OrgId}", userId, orgId);

        return orgId;
    }

    public async Task<bool> CheckOrganizationExistsAsync(
        string orgName,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"{_realmBase}/organizations?search={Uri.EscapeDataString(orgName)}&exact=true",
            ct);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        return document.RootElement.GetArrayLength() > 0;
    }

    public async Task<ExternalOrganizationDetails?> GetOrganizationDetailsAsync(
        string externalOrganizationId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"{_realmBase}/organizations/{externalOrganizationId}",
            ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Organization not found in Keycloak: {Id}", externalOrganizationId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var id = document.RootElement.GetProperty("id").GetString() ?? externalOrganizationId;
        var name = document.RootElement.GetProperty("name").GetString() ?? string.Empty;

        return new ExternalOrganizationDetails(name, id);
    }

    private static string ExtractIdFromLocation(HttpResponseMessage response, string resourceType)
    {
        var location = response.Headers.Location
            ?? throw new InvalidOperationException(
                $"Keycloak did not return a Location header when creating {resourceType}");

        return location.Segments[^1].TrimEnd('/');
    }
}
