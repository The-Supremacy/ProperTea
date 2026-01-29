using Marten.Events.Aggregation;

namespace ProperTea.User.Features.UserProfiles.Projections;

/// <summary>
/// Read model for detailed user profile views.
/// Projected from UserProfile events for efficient detail page queries.
/// </summary>
public class UserProfileDetailsView : SingleStreamProjection<UserProfileAggregate, Guid>
{
    public Guid Id { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? OrganizationDeactivatedAt { get; set; }
    public string? TenantId { get; set; }

    // TODO: Add denormalized data from related aggregates (FullName, Email, etc.)
}
