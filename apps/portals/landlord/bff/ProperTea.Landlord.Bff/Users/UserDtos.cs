namespace ProperTea.Landlord.Bff.Users;

public record UserProfileDto(
    string UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public record UserDetailsDto(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string DisplayName
);

public record UserPreferencesDto(string Theme, string Language);

public record UpdateUserPreferencesRequest(string Theme, string Language);
