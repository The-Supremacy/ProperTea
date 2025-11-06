using ProperTea.ProperIntegrationEvents;

namespace ProperTea.Identity.Kernel.IntegrationEvents;

/// <summary>
///     Published when a new user registers in the system.
///     Other services can listen to this event to perform their own setup.
/// </summary>
public record UserCreatedIntegrationEvent(Guid Id, DateTime OccurredAt, Guid UserId, DateTime CreatedAt)
    : IntegrationEvent(Id, OccurredAt)
{
    public Guid UserId { get; set; } = UserId;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = CreatedAt;

    public static string EventTypeName => "UserCreated";
    public override string EventType => EventTypeName;
}