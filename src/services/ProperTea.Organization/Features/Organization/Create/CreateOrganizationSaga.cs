using Marten;
using Wolverine;

namespace ProperTea.Organization.Features.Organization.Create;

public class CreateOrganizationSaga : Saga
{
    // Saga state
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ZitadelOrgId { get; set; }

    // Step 1: Initiate - Create in local database
    public (OrganizationInitiated, CreateInZitadel) Handle(
        CreateOrganization command,
        IDocumentSession session)
    {
        var organizationId = Guid.NewGuid();

        var initiated = new OrganizationInitiated(
            organizationId,
            command.Name,
            command.Slug,
            command.Tier,
            command.CreatedBy,
            DateTime.UtcNow
        );

        _ = session.Events.StartStream<Organization>(organizationId, initiated);

        var createInZitadel = new CreateInZitadel(
            organizationId,
            command.Name
        );

        return (initiated, createInZitadel);
    }

    public void Start(OrganizationInitiated e)
    {
        OrganizationId = e.OrganizationId;
        Name = e.Name;
        Slug = e.Slug;
    }

    // Step 2: Create in Zitadel (handler in separate file)
    // CreateInZitadelHandler will process this

    // Step 3: Handle Zitadel creation response
    public object Handle(ZitadelOrganizationCreated e, IDocumentSession session)
    {
        ZitadelOrgId = e.ZitadelOrgId;

        // Append event to stream
        _ = session.Events.Append(OrganizationId, e);

        // Activate organization
        return new ActivateOrganization(OrganizationId);
    }

    // Step 4: Activate organization
    public (OrganizationActivated, OrganizationCreated) Handle(
        ActivateOrganization command,
        IDocumentSession session)
    {
        var activated = new OrganizationActivated(
            OrganizationId,
            DateTime.UtcNow
        );

        _ = session.Events.Append(OrganizationId, activated);

        var integrationEvent = new OrganizationCreated(
            OrganizationId,
            Name,
            Slug,
            ZitadelOrgId!.Value
        );

        MarkCompleted();

        return (activated, integrationEvent);
    }
}

public record CreateInZitadel(Guid OrganizationId, string Name);
public record ActivateOrganization(Guid OrganizationId);
