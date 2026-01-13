using Marten.Metadata;
using ProperTea.ServiceDefaults.Exceptions;
using static ProperTea.User.Features.UserProfiles.UserProfileEvents;

namespace ProperTea.User.Features.UserProfiles;

public class UserProfileAggregate : IRevisioned
{
    public Guid Id { get; set; }
    public string ZitadelUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public int Version { get; set; }

    #region Factory Methods

    /// <summary>
    /// Creates a new user profile.
    /// </summary>
    public static Created Create(Guid profileId, string zitadelUserId)
    {
        if (string.IsNullOrWhiteSpace(zitadelUserId))
            throw new ValidationException(nameof(zitadelUserId), "ZITADEL User ID is required");

        return new Created(profileId, zitadelUserId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Updates the last seen timestamp.
    /// </summary>
    public LastSeenUpdated UpdateLastSeen()
    {
        return new LastSeenUpdated(Id, DateTimeOffset.UtcNow);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.ProfileId;
        ZitadelUserId = e.ZitadelUserId;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(LastSeenUpdated e)
    {
        LastSeenAt = e.LastSeenAt;
    }

    #endregion
}
