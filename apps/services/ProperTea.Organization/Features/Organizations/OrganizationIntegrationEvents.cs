using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationIntegrationEvents
{
    [MessageIdentity("organizations.registered.v1")]
    public record OrganizationRegistered(
        Guid OrganizationId,
        string Name,
        string Slug,
        string ExternalOrganizationId,
        DateTimeOffset RegisteredAt) : IOrganizationRegistered;

    [MessageIdentity("organizations.activated.v1")]
    public record OrganizationActivated(
        Guid OrganizationId,
        DateTimeOffset ActivatedAt) : IOrganizationActivated;
}
