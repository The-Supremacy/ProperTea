namespace ProperTea.Contracts.Events;

public interface IOrganizationRegistered
{
    public Guid OrganizationId { get; }
    public string Name { get; }
    public string ExternalOrganizationId { get; }
    public DateTimeOffset RegisteredAt { get; }
}

public interface IOrganizationDeactivated
{
    public Guid OrganizationId { get; }
    public string Reason { get; }
    public DateTimeOffset DeactivatedAt { get; }
}

public interface IOrganizationActivated
{
    public Guid OrganizationId { get; }
    public DateTimeOffset ActivatedAt { get; }
}

public interface IOrganizationDomainVerified
{
    public Guid OrganizationId { get; }
    public string EmailDomain { get; }
    public DateTimeOffset VerifiedAt { get; }
}
