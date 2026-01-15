using ProperTea.Contracts.Events;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationIntegrationEvents
{
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

    public class OrganizationDeactivated(
        Guid organizationId,
        string reason,
        DateTimeOffset deactivatedAt) : IOrganizationDeactivated
    {
        public Guid OrganizationId { get; } = organizationId;
        public string Reason { get; } = reason;
        public DateTimeOffset DeactivatedAt { get; } = deactivatedAt;
    }

    public class OrganizationActivated(
        Guid organizationId,
        DateTimeOffset activatedAt) : IOrganizationActivated
    {
        public Guid OrganizationId { get; } = organizationId;
        public DateTimeOffset ActivatedAt { get; } = activatedAt;
    }

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
