namespace ProperTea.Organization.Features.Organizations.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1");

        CreateOrganizationEndpoint.Map(v1);
    }
}
