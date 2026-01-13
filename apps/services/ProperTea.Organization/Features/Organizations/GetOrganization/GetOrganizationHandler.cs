using Marten;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.GetOrganization;

public record GetOrganizationQuery(Guid OrganizationId);

public static class GetOrganizationHandler
{
    public static async Task<OrganizationResponse> Handle(
        GetOrganizationQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var org = await session.LoadAsync<OrganizationAggregate>(query.OrganizationId, ct);

        return org is null
            ? throw new NotFoundException(nameof(OrganizationAggregate), query.OrganizationId)
            : OrganizationResponse.FromAggregate(org);
    }
}
