namespace ProperTea.Contracts.Events;

/// <summary>
/// Contract for user profile created integration event.
/// This interface defines the data contract only - no framework dependencies.
/// Each service can implement this with their own messaging framework.
/// </summary>
public interface IUserProfileCreated
{
    public Guid ProfileId { get; }
    public string ZitadelUserId { get; }
    public DateTimeOffset CreatedAt { get; }
}
