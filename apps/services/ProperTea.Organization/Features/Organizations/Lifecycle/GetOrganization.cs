using Marten;
using ProperTea.ServiceDefaults.Exceptions;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetOrganizationQuery(Guid OrganizationId);

public record GetOrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    OrganizationAggregate.Status Status,
    string? ExternalOrganizationId,
    DateTimeOffset CreatedAt,
    int Version)
{
    public static GetOrganizationResponse FromAggregate(OrganizationAggregate aggregate)
    {
        return new GetOrganizationResponse(
            aggregate.Id,
            aggregate.Name,
            aggregate.Slug,
            aggregate.CurrentStatus,
            aggregate.ExternalOrganizationId,
            aggregate.CreatedAt,
            aggregate.Version
        );
    }
}

public class GetOrganizationHandler : IWolverineHandler
{
    public async Task<GetOrganizationResponse> Handle(
        GetOrganizationQuery query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var org = await session.LoadAsync<OrganizationAggregate>(query.OrganizationId, ct);

        return org is null
            ? throw new NotFoundException(nameof(OrganizationAggregate), query.OrganizationId)
            : GetOrganizationResponse.FromAggregate(org);
    }
}
