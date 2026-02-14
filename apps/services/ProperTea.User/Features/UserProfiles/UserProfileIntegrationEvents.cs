using ProperTea.Contracts.Events;

namespace ProperTea.User.Features.UserProfiles;

public static class UserProfileIntegrationEvents
{
    public class UserProfileCreatedEvent(
        string userId,
        DateTimeOffset createdAt) : IUserProfileCreated
    {
        public string UserId { get; } = userId;
        public DateTimeOffset CreatedAt { get; } = createdAt;
    }
}
