namespace ProperTea.Landlord.Bff.Users;

/// <summary>
/// User profile DTOs matching backend contracts
/// </summary>
public record UserProfileDto(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);
