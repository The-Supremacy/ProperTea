namespace ProperTea.Company.Features.Companies;

public static class CompanyEvents
{
    public record Created(
        Guid CompanyId,
        string Name,
        DateTimeOffset CreatedAt);

    public record NameUpdated(
        Guid CompanyId,
        string Name);

    public record Deleted(
        Guid CompanyId,
        DateTimeOffset DeletedAt);
}
