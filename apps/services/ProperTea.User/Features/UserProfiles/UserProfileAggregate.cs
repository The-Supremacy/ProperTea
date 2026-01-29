using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.User.Features.UserProfiles.UserProfileEvents;

namespace ProperTea.User.Features.UserProfiles;

public class UserProfileAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? OrganizationDeactivatedAt { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(Guid profileId, string externalUserId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new BusinessViolationException(
                UserProfileErrorCodes.EXTERNAL_ID_REQUIRED,
                nameof(externalUserId),
                "External User ID is required");

        return new Created(profileId, externalUserId, DateTimeOffset.UtcNow);
    }

    public LastSeenUpdated UpdateLastSeen()
    {
        return new LastSeenUpdated(Id, DateTimeOffset.UtcNow);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.ProfileId;
        ExternalUserId = e.ExternalUserId;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(LastSeenUpdated e)
    {
        LastSeenAt = e.LastSeenAt;
    }

    public void Apply(OrganizationDeactivatedMarked e)
    {
        OrganizationDeactivatedAt = e.DeactivatedAt;
    }

    public void Apply(OrganizationDeactivatedCleared e)
    {
        OrganizationDeactivatedAt = null;
    }

    #endregion
}
