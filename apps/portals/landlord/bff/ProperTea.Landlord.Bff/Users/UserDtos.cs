namespace ProperTea.Landlord.Bff.Users;

public record UserProfileDto(
    Guid Id,
    string ExternalUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public record UserDetailsDto(
    Guid? InternalId,
    string ExternalId,
    string Email,
    string? FirstName,
    string? LastName,
    string DisplayName
);

public record UserPreferencesDto(string Theme, string Language);

public record UpdateUserPreferencesRequest(string Theme, string Language);
