namespace ProperTea.Organization.Infrastructure
{
    public record ExternalOrganizationDetails(string Name, string Id);

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

        public Task<ExternalOrganizationDetails?> GetOrganizationDetailsAsync(
            string externalOrganizationId,
            CancellationToken ct = default);
    }
}
