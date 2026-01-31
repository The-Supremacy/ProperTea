using Wolverine.Attributes;

namespace ProperTea.Company.Features.Companies;

public static class CompanyIntegrationEvents
{
    [MessageIdentity("companies.created.v1")]
    public class CompanyCreated
    {
        public Guid CompanyId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("companies.deleted.v1")]
    public class CompanyDeleted
    {
        public Guid CompanyId { get; set; }
        public Guid OrganizationId { get; set; }
        public DateTimeOffset DeletedAt { get; set; }
    }
}
