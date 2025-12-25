using Wolverine;

namespace ProperTea.Organization.Features.Organization.Create;

public static class CreateOrganizationEndpoints
{
    public static RouteGroupBuilder MapCreateOrganizationEndpoints(this RouteGroupBuilder group)
    {
        _ = group.MapPost("/", CreateOrganization).WithName("CreateOrganization").RequireAuthorization();

        return group;
    }

    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        IMessageBus bus,
        HttpContext context)
    {
        var userId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? Guid.NewGuid().ToString());

        var command = new CreateOrganization(
            request.Name,
            request.Slug ?? GenerateSlug(request.Name),
            request.Tier ?? SubscriptionTier.Trial,
            userId
        );

        var result = await bus.InvokeAsync<CreateOrganizationResult>(command);

        return Results.Created($"/organizations/{result.OrganizationId}", result);
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Trim('-');
    }
}

public record CreateOrganizationRequest(
    string Name,
    string? Slug = null,
    SubscriptionTier? Tier = null
);

public record CreateOrganizationResult(
    Guid OrganizationId,
    string Name,
    string Slug,
    Guid? ZitadelOrgId
);
