namespace ProperTea.Contracts.Events;

/// <summary>
/// Framework-agnostic interface contracts for Organization integration events.
/// Services can implement these using their own messaging framework.
/// </summary>
public interface IOrganizationRegistered
{
    public Guid OrganizationId { get; }
    public string Name { get; }
    public string Slug { get; }
    public string ZitadelOrganizationId { get; }
    public string? EmailDomain { get; }
    public DateTimeOffset RegisteredAt { get; }
}

public interface IOrganizationIdentityUpdated
{
    public Guid OrganizationId { get; }
    public string NewName { get; }
    public string NewSlug { get; }
    public DateTimeOffset UpdatedAt { get; }
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
