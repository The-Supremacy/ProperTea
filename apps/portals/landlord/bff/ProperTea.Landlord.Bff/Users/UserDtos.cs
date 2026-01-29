namespace ProperTea.Landlord.Bff.Users;

public record UserProfileDto(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);
