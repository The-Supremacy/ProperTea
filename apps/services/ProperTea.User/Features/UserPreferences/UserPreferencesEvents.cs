namespace ProperTea.User.Features.UserPreferences;

public static class UserPreferencesEvents
{
    public record PreferencesUpdated(
        string ExternalUserId,
        string Theme,
        string Language,
        DateTimeOffset UpdatedAt
    );
}
