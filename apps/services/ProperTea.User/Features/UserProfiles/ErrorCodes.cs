namespace ProperTea.User.Features.UserProfiles;

/// <summary>
/// Error codes for UserProfile domain.
/// Used for frontend i18n translation.
/// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores - Error codes use underscores by convention
public static class UserProfileErrorCodes
{
    // Domain errors
    public const string NOT_FOUND = "USER.NOT_FOUND";
    public const string EXTERNAL_ID_REQUIRED = "USER.EXTERNAL_ID_REQUIRED";

    // Validation errors
    public const string VALIDATION_EXTERNAL_ID_REQUIRED = "USER.VALIDATION.EXTERNAL_ID_REQUIRED";
}
#pragma warning restore CA1707
