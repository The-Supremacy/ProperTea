namespace ProperTea.Contracts.Events;

public interface IUserProfileCreated
{
    public string UserId { get; }
    public DateTimeOffset CreatedAt { get; }
}
