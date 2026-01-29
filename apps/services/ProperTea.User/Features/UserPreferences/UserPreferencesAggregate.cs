using Marten.Metadata;

namespace ProperTea.User.Features.UserPreferences;

public class UserPreferencesAggregate : IRevisioned
{
    public Guid Id { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public int Version { get; set; }

    public static UserPreferencesEvents.PreferencesUpdated UpdatePreferences(
        string externalUserId,
        string theme,
        string language)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new ArgumentException("External user ID cannot be empty", nameof(externalUserId));

        if (string.IsNullOrWhiteSpace(theme) || (theme != "light" && theme != "dark"))
            throw new ArgumentException("Theme must be 'light' or 'dark'", nameof(theme));

        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be empty", nameof(language));

        return new UserPreferencesEvents.PreferencesUpdated(
            externalUserId,
            theme,
            language,
            DateTimeOffset.UtcNow
        );
    }

    public void Apply(UserPreferencesEvents.PreferencesUpdated e)
    {
        ExternalUserId = e.ExternalUserId;
        Theme = e.Theme;
        Language = e.Language;
    }
}
