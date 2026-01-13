using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.GetMyOrganization;

public record GetMyOrganizationQuery;

public record MyOrganizationResponse(
    Guid LocalOrgId,
    string ZitadelOrgId,
    string Name
);

public class GetMyOrganizationHandler(
    IZitadelClient zitadelClient,
    IDocumentSession session)
{
    public async Task<MyOrganizationResponse> Handle(GetMyOrganizationQuery query, CancellationToken ct)
    {
        // Get current organization from ZITADEL context
        var zitadelOrgId = await zitadelClient.GetMyOrganizationIdAsync(ct);

        // Find the local organization by ZITADEL ID
        var localOrg = await session.Query<OrganizationAggregate>()
            .FirstOrDefaultAsync(x => x.ZitadelOrganizationId == zitadelOrgId, ct)
            ?? throw new NotFoundException("Organization", zitadelOrgId);

        return new MyOrganizationResponse(
            LocalOrgId: localOrg.Id,
            ZitadelOrgId: localOrg.ZitadelOrganizationId ?? throw new InvalidOperationException("Organization not linked to ZITADEL"),
            Name: localOrg.Name
        );
    }
}
