using Marten;
using Wolverine;
using ProperTea.Organization.Infrastructure;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetOrganizationByExternalId(string ExternalOrganizationId);

public class GetOrganizationByExternalIdHandler(
    IExternalOrganizationClient externalOrgClient) : IWolverineHandler
{
    public async Task<OrganizationResponse?> Handle(
        GetOrganizationByExternalId query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var organization = await session.Query<OrganizationAggregate>()
            .FirstOrDefaultAsync(x => x.ExternalOrganizationId == query.ExternalOrganizationId, ct);

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
