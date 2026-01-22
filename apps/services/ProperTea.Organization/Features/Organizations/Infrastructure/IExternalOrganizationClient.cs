namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public interface IExternalOrganizationClient
    {
        public Task<string> CreateOrganizationAsync(string orgName, string orgAlias, Dictionary<string, bool>? domains, CancellationToken ct = default);
        public Task UpdateOrganizationAsync(string externalOrgId, string newName, Dictionary<string, bool>? newDomains, CancellationToken ct = default);
        public Task AddUserToOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default);
        public Task RemoveUserFromOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default);
    }
}
