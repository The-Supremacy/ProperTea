namespace ProperTea.Contracts.Events;

public interface ICompanyCreated
{
    public Guid CompanyId { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface ICompanyUpdated
{
    public Guid CompanyId { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public interface ICompanyDeleted
{
    public Guid CompanyId { get; }
    public Guid OrganizationId { get; }
    public DateTimeOffset DeletedAt { get; }
}
