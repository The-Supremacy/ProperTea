namespace ProperTea.Core.Tenancy;

public interface ICurrentOrganizationProvider
{
    Guid? OrganizationId { get; }
    void SetOrganization(Guid organizationId);
}