using Microsoft.AspNetCore.Mvc;
using ProperTea.Core.Auth;
using ProperTea.Organization.Features.Organizations.Create;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        app.MapGet("/", (ICurrentUser user) => "Hello World! User Id: " + user.Id + "").RequireAuthorization();
        app.MapGet("/a", ([FromServices]ILogger<Program> logger) =>
        {
#pragma warning disable CA1848
            logger.LogError("YO");
#pragma warning restore CA1848
            return "Hello World!";
        });

        var v1 = app.MapGroup("/api/v1").RequireAuthorization();

        CreateOrganizationEndpoint.Map(v1);
    }
}
