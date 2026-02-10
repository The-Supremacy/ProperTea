using Marten;
using Wolverine;
using ProperTea.Organization.Infrastructure;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetOrganization(Guid OrganizationId);

public record OrganizationResponse(
    Guid Id,
    string? Name,
    string Status,
    string Tier,
    string? ExternalOrganizationId,
    DateTimeOffset CreatedAt);

public class GetOrganizationHandler(
    IExternalOrganizationClient externalOrgClient) : IWolverineHandler
{
    public async Task<OrganizationResponse?> Handle(
        GetOrganization query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var organization = await session.Events.AggregateStreamAsync<OrganizationAggregate>(query.OrganizationId, token: ct);

        if (organization == null)
            return null;

        string? name = null;
        if (organization.ExternalOrganizationId != null)
        {
            var externalOrg = await externalOrgClient.GetOrganizationDetailsAsync(
                organization.ExternalOrganizationId,
                ct);
            name = externalOrg?.Name;
        }

        return new OrganizationResponse(
            organization.Id,
            name,
            organization.CurrentStatus.ToString(),
            organization.CurrentTier.ToString(),
            organization.ExternalOrganizationId,
            organization.CreatedAt);
    }
}
