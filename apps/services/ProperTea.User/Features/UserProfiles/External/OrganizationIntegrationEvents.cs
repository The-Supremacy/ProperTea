using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.User.Features.UserProfiles.External;

public static class OrganizationIntegrationEvents
{
    [MessageIdentity("organizations.registered.v1")]
    public record OrganizationRegistered(
        string OrganizationId,
        string Name,
        DateTimeOffset RegisteredAt) : IOrganizationRegistered;

    [MessageIdentity("organizations.deactivated.v1")]
    public record OrganizationDeactivated(
        string OrganizationId,
        string Reason,
        DateTimeOffset DeactivatedAt) : IOrganizationDeactivated;

    [MessageIdentity("organizations.activated.v1")]
    public record OrganizationActivated(
        string OrganizationId,
        DateTimeOffset ActivatedAt) : IOrganizationActivated;

    [MessageIdentity("organizations.domain-verified.v1")]
    public record OrganizationDomainVerified(
        string OrganizationId,
        string EmailDomain,
        DateTimeOffset VerifiedAt) : IOrganizationDomainVerified;
}
