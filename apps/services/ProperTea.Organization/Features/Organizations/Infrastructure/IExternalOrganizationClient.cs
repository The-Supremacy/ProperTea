namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public interface IExternalOrganizationClient
    {
        public Task<string> CreateOrganizationWithAdminAsync(
            string orgName,
            string email,
            string firstName,
            string lastName,
            CancellationToken ct = default);
    }
}
