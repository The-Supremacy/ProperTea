using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record GetMyOrganizationQuery;

public record MyOrganizationResponse(
    Guid Id,
    string ExternalOrgId,
    string Name
);

public class GetMyOrganizationHandler(
    IExternalOrganizationClient externalOrgClient,
    IDocumentSession session)
{
    public async Task<MyOrganizationResponse> Handle(GetMyOrganizationQuery query, CancellationToken ct)
    {
        var zitadelOrgId = await externalOrgClient.GetMyOrganizationIdAsync(ct);

        var localOrg = await session.Query<OrganizationAggregate>()
            .FirstOrDefaultAsync(x => x.ExternalOrganizationId == zitadelOrgId, ct)
            ?? throw new NotFoundException("Organization", zitadelOrgId);

        return new MyOrganizationResponse(
            Id: localOrg.Id,
            ExternalOrgId: localOrg.ExternalOrganizationId ?? throw new InvalidOperationException("Organization not linked to external organization"),
            Name: localOrg.Name
        );
    }
}
