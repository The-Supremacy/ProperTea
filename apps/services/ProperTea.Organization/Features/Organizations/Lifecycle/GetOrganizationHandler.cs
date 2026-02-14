using Marten;
using Wolverine;
using ProperTea.Organization.Infrastructure;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetOrganization(string OrganizationId);

public record OrganizationResponse(
    string OrganizationId,
    string? Name,
    string Status,
    string Tier,
    DateTimeOffset CreatedAt);

public class GetOrganizationHandler(
    IExternalOrganizationClient externalOrgClient) : IWolverineHandler
{
    public async Task<OrganizationResponse?> Handle(
        GetOrganization query,
        IDocumentSession session,
        CancellationToken ct)
    {
        var organization = await session.Query<OrganizationAggregate>()
            .FirstOrDefaultAsync(x => x.OrganizationId == query.OrganizationId, ct);

        if (organization == null)
            return null;

        string? name = null;
        if (organization.OrganizationId != null)
        {
            var externalOrg = await externalOrgClient.GetOrganizationDetailsAsync(
                organization.OrganizationId,
                ct);
            name = externalOrg?.Name;
        }

        return new OrganizationResponse(
            organization.OrganizationId!,
            name,
            organization.CurrentStatus.ToString(),
            organization.CurrentTier.ToString(),
            organization.CreatedAt);
    }
}
