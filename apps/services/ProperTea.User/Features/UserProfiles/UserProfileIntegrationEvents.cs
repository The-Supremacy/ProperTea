using ProperTea.Contracts.Events;

namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileIntegrationEvents
{
    public class UserProfileCreatedEvent(
        Guid profileId,
        string externalUserId,
        DateTimeOffset createdAt) : IUserProfileCreated
    {
        public Guid ProfileId { get; } = profileId;
        public string ExternalUserId { get; } = externalUserId;
        public DateTimeOffset CreatedAt { get; } = createdAt;
    }
}
