namespace ProperTea.Contracts.Events;

public interface IOrganizationRegistered
{
    public string OrganizationId { get; }
    public string Name { get; }
    public DateTimeOffset RegisteredAt { get; }
}

public interface IOrganizationDeactivated
{
    public string OrganizationId { get; }
    public string Reason { get; }
    public DateTimeOffset DeactivatedAt { get; }
}

public interface IOrganizationActivated
{
    public string OrganizationId { get; }
    public DateTimeOffset ActivatedAt { get; }
}

public interface IOrganizationDomainVerified
{
    public string OrganizationId { get; }
    public string EmailDomain { get; }
    public DateTimeOffset VerifiedAt { get; }
}
