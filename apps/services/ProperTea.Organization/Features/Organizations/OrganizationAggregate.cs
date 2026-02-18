using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations;

public class OrganizationAggregate : IRevisioned
{
    public Guid Id { get; set; }
    public Status CurrentStatus { get; set; }
    public SubscriptionTier CurrentTier { get; set; }
    public string? OrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    #region Deciders
    public static Created Create(Guid id)
    {
        return new Created(id, SubscriptionTier.Trial, DateTimeOffset.UtcNow);
    }

    public static OrganizationLinked LinkExternalOrganization(Guid streamId, string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new BusinessViolationException(
                OrganizationErrorCodes.EXTERNAL_ID_REQUIRED,
                nameof(organizationId),
                "Organization ID is required");

        return new OrganizationLinked(streamId, organizationId);
    }

    public Activated Activate()
    {
        return new Activated(Id, DateTimeOffset.UtcNow);
    }

    #endregion

    #region Event Appliers
    public void Apply(Created e)
    {
        Id = e.OrganizationId;
        CurrentStatus = Status.Pending;
        CreatedAt = e.CreatedAt;
        CurrentTier = e.Tier;
    }

    public void Apply(OrganizationLinked e)
    {
        OrganizationId = e.OrganizationId;
    }

    public void Apply(Activated e)
    {
        CurrentStatus = Status.Active;
    }

    #endregion

    public enum Status
    {
        Pending = 1,
        Active = 2,
        Deactivated = 3
    }

    public enum SubscriptionTier
    {
        Trial = 1,
        Basic = 2,
    }
}
