namespace ProperTea.Landlord.Bff.Users;

public record UserProfileDto(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public record UserPreferencesDto(string Theme, string Language);

public record UpdateUserPreferencesRequest(string Theme, string Language);
