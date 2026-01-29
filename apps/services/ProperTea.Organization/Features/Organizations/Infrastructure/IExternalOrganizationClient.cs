namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public interface IExternalOrganizationClient
    {
        public Task<string> CreateOrganizationWithAdminAsync(
            string orgName,
            string email,
            string firstName,
            string lastName,
            string password,
            CancellationToken ct = default);

        public Task<bool> CheckOrganizationExistsAsync(
            string orgName,
            CancellationToken ct = default);
    }
}
