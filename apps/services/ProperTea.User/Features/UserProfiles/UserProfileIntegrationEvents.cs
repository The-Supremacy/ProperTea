using ProperTea.Contracts.Events;

namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileIntegrationEvents
{
    public class UserProfileCreatedEvent(
        Guid profileId,
        string zitadelUserId,
        DateTimeOffset createdAt) : IUserProfileCreated
    {
        public Guid ProfileId { get; } = profileId;
        public string ZitadelUserId { get; } = zitadelUserId;
        public DateTimeOffset CreatedAt { get; } = createdAt;
    }
}
