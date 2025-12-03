using ProperTea.Organization.Features.Organizations.Create;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1").RequireAuthorization();

        CreateOrganizationEndpoint.Map(v1);
    }
}
