using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Kiota.Abstractions;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Infrastructure;

public class KeycloakOrganizationClient(
    KeycloakAdminApiClient keycloakClient,
    IConfiguration configuration,
    ILogger<KeycloakOrganizationClient> logger)
    : IExternalOrganizationClient
{
    private string Realm => configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Realm config missing");

    public async Task<string> CreateOrganizationAsync(
        string orgName,
        string orgAlias,
        Dictionary<string, bool>? domains,
        CancellationToken ct = default)
    {
        var org = new OrganizationRepresentation
        {
            Name = orgName,
            Alias = orgAlias,
            Domains = domains?.Select(d => new OrganizationDomainRepresentation
            {
                Name = d.Key,
                Verified = d.Value
            }).ToList() ?? []
        };

        await keycloakClient.Admin.Realms[Realm].Organizations.PostAsync(org, cancellationToken: ct);

        var createdOrgs = await keycloakClient.Admin.Realms[Realm].Organizations.GetAsync(config =>
        {
            config.QueryParameters.Search = orgAlias;
        }, cancellationToken: ct);

        var actualOrg = createdOrgs?.FirstOrDefault(x => x.Alias == orgAlias)
            ?? throw new InvalidOperationException($"Failed to retrieve created organization with alias {orgAlias}");

        return actualOrg.Id!;
    }

    public async Task UpdateOrganizationAsync(
        string externalOrgId,
        string newName,
        Dictionary<string, bool>? newDomains,
        CancellationToken ct = default)
    {
        var org = await keycloakClient.Admin.Realms[Realm].Organizations[externalOrgId].GetAsync(cancellationToken: ct)
            ?? throw new NotFoundException($"Org {externalOrgId} not found");

        org.Name = newName;

        if (newDomains != null)
        {
            org.Domains = [.. newDomains.Select(d => new OrganizationDomainRepresentation
            {
                Name = d.Key,
                Verified = d.Value
            })];
        }

        await keycloakClient.Admin.Realms[Realm].Organizations[externalOrgId].PutAsync(org, cancellationToken: ct);
    }

    public async Task AddUserToOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default)
    {
        await keycloakClient.Admin.Realms[Realm].Organizations[externalOrgId].Members.PostAsync(userId, cancellationToken: ct);

        if (roles == null || roles.Length == 0) return;

        var rolesToAdd = new List<RoleRepresentation>();

        foreach (var roleName in roles)
        {
            try
            {
                var role = await keycloakClient.Admin.Realms[Realm].Roles[roleName].GetAsync(cancellationToken: ct);
                if (role != null)
                {
                    rolesToAdd.Add(new RoleRepresentation { Id = role.Id, Name = role.Name });
                }
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == 404)
            {
                logger.LogWarning("Role '{RoleName}' not found in Realm '{Realm}'. Ensure it is created as a Realm Role.", roleName, Realm);
            }
        }

        if (rolesToAdd.Count > 0)
        {
            await keycloakClient.Admin.Realms[Realm].Users[userId].RoleMappings.Realm.PostAsync(rolesToAdd, cancellationToken: ct);
        }
    }

    public async Task RemoveUserFromOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default)
    {
        await keycloakClient.Admin.Realms[Realm].Organizations[externalOrgId].Members[userId].DeleteAsync(cancellationToken: ct);
    }
}
