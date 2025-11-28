using ProperTea.Core.Tenancy;

namespace ProperTea.Infrastructure.Tenancy;

internal sealed class CurrentOrganizationProvider : ICurrentOrganizationProvider
{
    public Guid? OrganizationId { get; private set; }

    public void SetOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));

        OrganizationId = organizationId;
    }
}
