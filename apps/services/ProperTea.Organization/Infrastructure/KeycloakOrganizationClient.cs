using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Kiota.Abstractions;

namespace ProperTea.Organization.Infrastructure;

public class KeycloakOrganizationClient(
    KeycloakAdminApiClient adminApiClient,
    IConfiguration configuration,
    ILogger<KeycloakOrganizationClient> logger) : IExternalOrganizationClient
{
    public string Realm { get; } = configuration["Keycloak:Realm"]
        ?? throw new InvalidOperationException("Keycloak:Realm not configured");

    public async Task<string> CreateOrganizationWithAdminAsync(
        string orgName,
        string email,
        string firstName,
        string lastName,
        string password,
        CancellationToken ct = default)
    {
        var alias = orgName.ToLowerInvariant().Replace(" ", "-");
        await adminApiClient.Admin.Realms[Realm].Organizations.PostAsync(
            new OrganizationRepresentation { Name = orgName, Alias = alias, Enabled = true },
            cancellationToken: ct);

        var orgs = await adminApiClient.Admin.Realms[Realm].Organizations.GetAsync(
            q =>
            {
                q.QueryParameters.Search = orgName;
                q.QueryParameters.Exact = true;
            }, ct);

        var org = orgs?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Created organization not found by name: {orgName}");
        var orgId = org.Id ?? throw new InvalidOperationException("Created organization has no ID.");

        logger.LogInformation("Created Keycloak organization {Name} with ID {OrgId}", orgName, orgId);

        await adminApiClient.Admin.Realms[Realm].Users.PostAsync(
            new UserRepresentation
            {
                Username = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = true,
                Credentials =
                [
                    new CredentialRepresentation { Type = "password", Value = password, Temporary = false }
                ]
            }, cancellationToken: ct);

        var users = await adminApiClient.Admin.Realms[Realm].Users.GetAsync(
            q =>
            {
                q.QueryParameters.Email = email;
                q.QueryParameters.Exact = true;
            }, ct);

        var user = users?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Created user not found by email: {email}");
        var userId = user.Id ?? throw new InvalidOperationException("Created user has no ID.");

        logger.LogInformation("Created Keycloak user {Email} with ID {UserId}", email, userId);

        await adminApiClient.Admin.Realms[Realm].Organizations[orgId].Members.PostAsync(userId, cancellationToken: ct);

        logger.LogInformation("Added user {UserId} to organization {OrgId}", userId, orgId);

        return orgId;
    }

    public async Task<bool> CheckOrganizationExistsAsync(
        string orgName,
        CancellationToken ct = default)
    {
        var orgs = await adminApiClient.Admin.Realms[Realm].Organizations.GetAsync(
            q =>
            {
                q.QueryParameters.Search = orgName;
                q.QueryParameters.Exact = true;
            }, ct);

        return orgs is { Count: > 0 };
    }

    public async Task<ExternalOrganizationDetails?> GetOrganizationDetailsAsync(
        string externalOrganizationId,
        CancellationToken ct = default)
    {
        try
        {
            var org = await adminApiClient.Admin.Realms[Realm].Organizations[externalOrganizationId]
                .GetAsync(cancellationToken: ct);

            if (org is null) return null;

            return new ExternalOrganizationDetails(
                org.Name ?? string.Empty,
                org.Id ?? externalOrganizationId);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            logger.LogWarning("Organization not found in Keycloak: {Id}", externalOrganizationId);
            return null;
        }
    }
}

