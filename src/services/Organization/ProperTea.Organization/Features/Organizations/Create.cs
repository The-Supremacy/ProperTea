using Marten;
using ProperTea.Organization.Core;
using ProperTea.Utilities;
using Wolverine.Http;

namespace ProperTea.Organization.Features.Organizations;

public record CreateOrganization(string Name);

public static class CreateOrganizationEndpoint
{
    [WolverinePost("/api/v1/organizations")]
    public static IResult Handle(
        CreateOrganization command,
        IDocumentSession session)
    {
        var organization = new Core.Organization
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Alias = SlugGenerator.Generate(command.Name),
            Status = OrganizationStatus.Pending
        };
        session.Store(organization);
        return Results.Created($"/api/v1/organizations/{organization.Id}", organization.Id);
    }
}
