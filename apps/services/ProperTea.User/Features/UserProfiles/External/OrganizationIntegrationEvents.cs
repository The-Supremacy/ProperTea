using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.User.Features.UserProfiles.External;

public static class OrganizationIntegrationEvents
{
    [MessageIdentity("organizations.registered.v1")]
    public record OrganizationRegistered(
        Guid OrganizationId,
        string Name,
        string Slug,
        string ZitadelOrganizationId,
        string? EmailDomain,
        DateTimeOffset RegisteredAt) : IOrganizationRegistered;

    [MessageIdentity("organizations.identity-updated.v1")]
    public record OrganizationIdentityUpdated(
        Guid OrganizationId,
        string NewName,
        string NewSlug,
        DateTimeOffset UpdatedAt) : IOrganizationIdentityUpdated;

    [MessageIdentity("organizations.deactivated.v1")]
    public record OrganizationDeactivated(
        Guid OrganizationId,
        string Reason,
        DateTimeOffset DeactivatedAt) : IOrganizationDeactivated;

    [MessageIdentity("organizations.activated.v1")]
    public record OrganizationActivated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt) : IOrganizationActivated;

    [MessageIdentity("organizations.domain-verified.v1")]
    public record OrganizationDomainVerified(
        Guid OrganizationId,
        string EmailDomain,
        DateTimeOffset VerifiedAt) : IOrganizationDomainVerified;
}
