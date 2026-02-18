using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationIntegrationEvents
{
    [MessageIdentity("organizations.registered.v1")]
    public record OrganizationRegistered(
        string OrganizationId,
        string Name,
        DateTimeOffset RegisteredAt) : IOrganizationRegistered;

    [MessageIdentity("organizations.activated.v1")]
    public record OrganizationActivated(
        string OrganizationId,
        DateTimeOffset ActivatedAt) : IOrganizationActivated;
}
