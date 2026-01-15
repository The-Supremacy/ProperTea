using Marten;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetOrganizationQuery(Guid OrganizationId);

public record GetOrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    OrganizationAggregate.Status Status,
    string? ZitadelOrganizationId,
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
            aggregate.ZitadelOrganizationId,
            aggregate.CreatedAt,
            aggregate.Version
        );
    }
}

public static class GetOrganizationHandler
{
    public static async Task<GetOrganizationResponse> Handle(
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
