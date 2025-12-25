using ProperTea.Organization.Features.Organization.Create;

namespace ProperTea.Organization.Features.Organization;

public class Organization
{
    public Guid Id { get; private set; }
    public Guid? ExternalOrgId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public SubscriptionTier Tier { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public void Apply(OrganizationInitiated e)
    {
        Id = e.OrganizationId;
        Name = e.Name;
        Slug = e.Slug;
        Tier = e.Tier;
        Status = OrganizationStatus.Initializing;
        CreatedAt = e.Timestamp;
        CreatedBy = e.CreatedBy;
    }

    public void Apply(ZitadelOrganizationCreated e)
    {
        ExternalOrgId = e.ZitadelOrgId;
    }

    public void Apply(OrganizationActivated e)
    {
        Status = OrganizationStatus.Active;
    }
}

public enum SubscriptionTier
{
    Trial,
    Starter,
    Professional,
    Enterprise
}

public enum OrganizationStatus
{
    Initializing,
    Active,
    Suspended
}
