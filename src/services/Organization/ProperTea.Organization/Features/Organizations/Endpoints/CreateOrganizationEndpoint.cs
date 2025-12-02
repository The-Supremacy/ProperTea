using Wolverine.Http;

namespace ProperTea.Organization.Features.Organizations.Endpoints;

public static class CreateOrganizationEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPostToWolverine<CreateOrganization>("/api/v1/organizations")
            .WithTags("Organizations")
            .WithName("CreateOrganization");
    }
}
