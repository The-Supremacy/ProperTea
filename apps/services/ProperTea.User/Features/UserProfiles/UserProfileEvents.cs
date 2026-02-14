namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileEvents
{
    public record Created(
        Guid ProfileId,
        string UserId,
        DateTimeOffset CreatedAt);

    public record LastSeenUpdated(
        Guid ProfileId,
        DateTimeOffset LastSeenAt);

    public record OrganizationDeactivatedMarked(
        Guid ProfileId,
        DateTimeOffset DeactivatedAt);

    public record OrganizationDeactivatedCleared(
        Guid ProfileId);
}
