using Marten.Metadata;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations;

public class OrganizationAggregate : IRevisioned
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Status CurrentStatus { get; private set; }
    public string? ZitadelOrganizationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int Version { get; set; }

    public void Apply(Created e)
    {
        Id = e.OrganizationId;
        Name = e.Name;
        Slug = e.Slug;
        CurrentStatus = Status.Pending;
        CreatedAt = e.CreatedAt;
    }
    public void Apply(ZitadelProvisioningSucceeded e)
    {
        ZitadelOrganizationId = e.ZitadelOrganizationId;
    }
    public void Apply(ZitadelProvisioningFailed e)
    {
        CurrentStatus = Status.ProvisioningFailed;
    }

    public void Apply(Activated e)
    {
        CurrentStatus = Status.Active;
    }

    public void Apply(ActivationFailed e)
    {
        CurrentStatus = Status.ProvisioningFailed;
    }

    public enum Status
    {
        Pending = 1,
        Active = 2,
        ProvisioningFailed = 3,
        Deactivated = 4
    }

    public enum SubscriptionTier
    {
        Demo = 1,
        Trial = 2,
        Active = 3,
        Deactivated = 4
    }
}
