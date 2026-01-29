using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Organization.Features.Organizations.OrganizationEvents;

namespace ProperTea.Organization.Features.Organizations;

public class OrganizationAggregate : IRevisioned
{
    public Guid Id { get; set; }
    public Status CurrentStatus { get; set; }
    public SubscriptionTier CurrentTier { get; set; }
    public string? ExternalOrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    #region Factory Methods
    public static Created Create(Guid id)
    {
        return new Created(id, SubscriptionTier.Trial, DateTimeOffset.UtcNow);
    }

    public static ExternalOrganizationCreated LinkExternalOrganization(Guid organizationId, string externalOrganizationId)
    {
        if (string.IsNullOrWhiteSpace(externalOrganizationId))
            throw new BusinessViolationException(
                OrganizationErrorCodes.EXTERNAL_ID_REQUIRED,
                nameof(externalOrganizationId),
                "External Organization ID is required");

        return new ExternalOrganizationCreated(organizationId, externalOrganizationId);
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

    public void Apply(ExternalOrganizationCreated e)
    {
        ExternalOrganizationId = e.ExternalOrganizationId;
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
