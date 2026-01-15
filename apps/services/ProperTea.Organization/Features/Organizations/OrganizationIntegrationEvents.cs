using ProperTea.Contracts.Events;

namespace ProperTea.Organization.Features.Organizations;

/// <summary>
/// Integration events for Organization bounded context.
/// These implement framework-agnostic interfaces from Contracts.
/// Published to RabbitMQ via IMessageBus.PublishAsync() for cross-service communication.
/// </summary>
public static class OrganizationIntegrationEvents
{
    /// <summary>
    /// Published when an organization completes registration and becomes active.
    /// Other services can subscribe to provision tenant-specific resources.
    /// </summary>
    public class OrganizationRegistered(
        Guid organizationId,
        string name,
        string slug,
        string zitadelOrganizationId,
        string? emailDomain,
        DateTimeOffset registeredAt) : IOrganizationRegistered
    {
        public Guid OrganizationId { get; } = organizationId;
        public string Name { get; } = name;
        public string Slug { get; } = slug;
        public string ZitadelOrganizationId { get; } = zitadelOrganizationId;
        public string? EmailDomain { get; } = emailDomain;
        public DateTimeOffset RegisteredAt { get; } = registeredAt;
    }

    /// <summary>
    /// Published when an organization's identity (name/slug) is updated.
    /// Allows other services to sync display names, URLs, etc.
    /// </summary>
    public class OrganizationIdentityUpdated(
        Guid organizationId,
        string newName,
        string newSlug,
        DateTimeOffset updatedAt) : IOrganizationIdentityUpdated
    {
        public Guid OrganizationId { get; } = organizationId;
        public string NewName { get; } = newName;
        public string NewSlug { get; } = newSlug;
        public DateTimeOffset UpdatedAt { get; } = updatedAt;
    }

    /// <summary>
    /// Published when an organization is deactivated.
    /// Triggers cleanup/archival workflows in downstream services.
    /// </summary>
    public class OrganizationDeactivated(
        Guid organizationId,
        string reason,
        DateTimeOffset deactivatedAt) : IOrganizationDeactivated
    {
        public Guid OrganizationId { get; } = organizationId;
        public string Reason { get; } = reason;
        public DateTimeOffset DeactivatedAt { get; } = deactivatedAt;
    }

    /// <summary>
    /// Published when an organization's email domain is verified in ZITADEL.
    /// Enables auto-join functionality for users with matching email domains.
    /// </summary>
    public class OrganizationDomainVerified(
        Guid organizationId,
        string emailDomain,
        DateTimeOffset verifiedAt) : IOrganizationDomainVerified
    {
        public Guid OrganizationId { get; } = organizationId;
        public string EmailDomain { get; } = emailDomain;
        public DateTimeOffset VerifiedAt { get; } = verifiedAt;
    }
}
