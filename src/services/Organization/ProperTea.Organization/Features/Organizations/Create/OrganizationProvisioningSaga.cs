using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace ProperTea.Organization.Features.Organizations.Create;

public record StartOrganizationProvisioning(Guid Id, Guid CreatorUserId);

[MessageIdentity("organization-created-v1")]
public record OrganizationProvisioned(Guid OrganizationId, string Name, string OrgAlias);

public class OrganizationProvisioningSaga : Saga
{
    public Guid Id { get; set; }

    public static (OrganizationProvisioningSaga, CreateKeycloakOrganization) Start(
        StartOrganizationProvisioning message)
    {
        return (
            new OrganizationProvisioningSaga { Id = message.Id },
            new CreateKeycloakOrganization(message.Id, message.CreatorUserId)
            );
    }

    public OrganizationProvisioned Handle(
        KeycloakOrganizationCreated message,
        [Entity] Domain.Organization organization)
    {
        MarkCompleted();

        return new OrganizationProvisioned(this.Id, organization.Name, organization.OrgAlias);
    }

    public RemoveLocalOrganization Handle(KeycloakOrganizationCreationFailed message)
    {
        MarkCompleted();

        return new RemoveLocalOrganization(this.Id);
    }
}
