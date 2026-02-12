namespace ProperTea.Company.Features.Companies;

#pragma warning disable CA1707 // Identifiers should not contain underscores - Error codes use underscores by convention
public static class CompanyErrorCodes
{
    public const string COMPANY_NOT_FOUND = "COMPANY_NOT_FOUND";
    public const string COMPANY_NAME_REQUIRED = "COMPANY_NAME_REQUIRED";
    public const string COMPANY_CODE_REQUIRED = "COMPANY_CODE_REQUIRED";
    public const string COMPANY_CODE_TOO_LONG = "COMPANY_CODE_TOO_LONG";
    public const string COMPANY_CODE_ALREADY_EXISTS = "COMPANY_CODE_ALREADY_EXISTS";
    public const string COMPANY_ALREADY_DELETED = "COMPANY_ALREADY_DELETED";
}
#pragma warning restore CA1707
