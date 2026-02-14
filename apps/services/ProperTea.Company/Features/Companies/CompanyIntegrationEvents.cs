using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Company.Features.Companies;

public static class CompanyIntegrationEvents
{
    [MessageIdentity("companies.created.v1")]
    public class CompanyCreated : ICompanyCreated
    {
        public Guid CompanyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("companies.updated.v1")]
    public class CompanyUpdated : ICompanyUpdated
    {
        public Guid CompanyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [MessageIdentity("companies.deleted.v1")]
    public class CompanyDeleted : ICompanyDeleted
    {
        public Guid CompanyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public DateTimeOffset DeletedAt { get; set; }
    }
}
