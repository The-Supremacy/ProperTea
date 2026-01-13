namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileEvents
{
    public record Created(
        Guid ProfileId,
        string ZitadelUserId,
        DateTimeOffset CreatedAt);

    public record LastSeenUpdated(
        Guid ProfileId,
        DateTimeOffset LastSeenAt);
}
