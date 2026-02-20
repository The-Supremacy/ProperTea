namespace ProperTea.Company.Features.Companies;

public static class CompanyEvents
{
    public record Created(
        Guid CompanyId,
        string Code,
        string Name,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(
        Guid CompanyId,
        string Code,
        DateTimeOffset UpdatedAt);

    public record NameUpdated(
        Guid CompanyId,
        string Name,
        DateTimeOffset UpdatedAt);

    public record Deleted(
        Guid CompanyId,
        DateTimeOffset DeletedAt);
}
