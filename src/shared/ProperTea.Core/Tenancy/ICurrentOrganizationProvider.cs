namespace ProperTea.Core.Tenancy;

public interface ICurrentOrganizationProvider
{
    public Guid? OrganizationId { get; }
    public void SetOrganization(Guid organizationId);
}
