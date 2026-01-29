using Marten.Events.Aggregation;

namespace ProperTea.User.Features.UserProfiles.Projections;

/// <summary>
/// Read model for user profile list views.
/// Projected from UserProfile events for efficient querying.
/// </summary>
public class UserProfileListView : SingleStreamProjection<UserProfileAggregate, Guid>
{
    public Guid Id { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string? TenantId { get; set; }

    // TODO: Add denormalized data for list queries (FullName, Email, RoleName, OrganizationName, etc.)
}
