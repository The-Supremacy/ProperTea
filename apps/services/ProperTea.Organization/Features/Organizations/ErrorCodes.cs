namespace ProperTea.Organization.Features.Organizations;

/// <summary>
/// Error codes for Organization domain.
/// Used for frontend i18n translation.
/// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores - Error codes use underscores by convention
public static class OrganizationErrorCodes
{
    // Domain errors
    public const string NAME_ALREADY_EXISTS = "ORG.NAME_ALREADY_EXISTS";
    public const string NOT_FOUND = "ORG.NOT_FOUND";
    public const string EXTERNAL_ID_REQUIRED = "ORG.EXTERNAL_ID_REQUIRED";

    // Validation errors
    public const string VALIDATION_NAME_REQUIRED = "ORG.VALIDATION.NAME_REQUIRED";
    public const string VALIDATION_NAME_TOO_SHORT = "ORG.VALIDATION.NAME_TOO_SHORT";
    public const string VALIDATION_NAME_TOO_LONG = "ORG.VALIDATION.NAME_TOO_LONG";

    public const string VALIDATION_EMAIL_REQUIRED = "ORG.VALIDATION.EMAIL_REQUIRED";
    public const string VALIDATION_EMAIL_INVALID = "ORG.VALIDATION.EMAIL_INVALID";

    public const string VALIDATION_FIRST_NAME_REQUIRED = "ORG.VALIDATION.FIRST_NAME_REQUIRED";
    public const string VALIDATION_FIRST_NAME_TOO_SHORT = "ORG.VALIDATION.FIRST_NAME_TOO_SHORT";
    public const string VALIDATION_FIRST_NAME_TOO_LONG = "ORG.VALIDATION.FIRST_NAME_TOO_LONG";

    public const string VALIDATION_LAST_NAME_REQUIRED = "ORG.VALIDATION.LAST_NAME_REQUIRED";
    public const string VALIDATION_LAST_NAME_TOO_SHORT = "ORG.VALIDATION.LAST_NAME_TOO_SHORT";
    public const string VALIDATION_LAST_NAME_TOO_LONG = "ORG.VALIDATION.LAST_NAME_TOO_LONG";

    public const string VALIDATION_PASSWORD_REQUIRED = "ORG.VALIDATION.PASSWORD_REQUIRED";
    public const string VALIDATION_PASSWORD_TOO_SHORT = "ORG.VALIDATION.PASSWORD_TOO_SHORT";
    public const string VALIDATION_PASSWORD_TOO_LONG = "ORG.VALIDATION.PASSWORD_TOO_LONG";
    public const string VALIDATION_PASSWORD_MISSING_LOWERCASE = "ORG.VALIDATION.PASSWORD_MISSING_LOWERCASE";
    public const string VALIDATION_PASSWORD_MISSING_UPPERCASE = "ORG.VALIDATION.PASSWORD_MISSING_UPPERCASE";
    public const string VALIDATION_PASSWORD_MISSING_NUMBER = "ORG.VALIDATION.PASSWORD_MISSING_NUMBER";
    public const string VALIDATION_PASSWORD_MISSING_SPECIAL = "ORG.VALIDATION.PASSWORD_MISSING_SPECIAL";
}
#pragma warning restore CA1707
