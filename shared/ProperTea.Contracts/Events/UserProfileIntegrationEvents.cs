namespace ProperTea.Contracts.Events;

public interface IUserProfileCreated
{
    public Guid ProfileId { get; }
    public string ExternalUserId { get; }
    public DateTimeOffset CreatedAt { get; }
}
