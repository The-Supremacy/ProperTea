namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public interface IExternalOrganizationClient
    {
        public Task<string> CreateOrganizationAsync(string orgName, CancellationToken ct = default);
        public Task UpdateOrganizationAsync(string externalOrgId, string newName, CancellationToken ct = default);
        public Task AddUserToOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default);

        public Task<string> GetMyOrganizationIdAsync(CancellationToken ct = default);
        public Task RemoveUserFromOrganizationAsync(string externalOrgId, string userId, CancellationToken ct = default);
        public Task VerifyOrgDomainAsync(string externalOrgId, string domain, CancellationToken ct = default);
    }
}
