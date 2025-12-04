using Wolverine;
using Wolverine.Attributes;

namespace ProperTea.Organization.Features.Organizations.Create;

[MessageIdentity("organization-created-v1")]
public record OrganizationProvisioned(Guid OrganizationId, string Name, string OrgAlias);

public class OrganizationProvisioningSaga : Saga
{
    public Guid Id { get; set; }

    public static OrganizationProvisioningSaga Start(
        LocalOrganizationCreated message)
    {
        return (
            new OrganizationProvisioningSaga { Id = message.OrganizationId }
            );
    }
}
