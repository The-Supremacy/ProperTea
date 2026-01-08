using Wolverine;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations")
            .WithTags("Organizations");
        _ = group.MapPost("/", RegisterOrganization)
        .WithName("RegisterOrganization").RequireAuthorization();

        return group;
    }

    private static async Task<IResult> RegisterOrganization(
        CreateOrganizationRequest request,
        IMessageBus bus,
        HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value ?? string.Empty;

        var command = new OrganizationMessages.StartRegistration(
            Guid.NewGuid(),
            request.Name,
            request.Slug!
        );

        var result = await bus.InvokeAsync<OrganizationEvents.Activated>(command);

        return Results.Created($"/organizations/{result.OrganizationId}", new CreateOrganizationResult(
            result.OrganizationId
        ));
    }
}

public record CreateOrganizationRequest(
    string Name,
    string? Slug = null,
    OrganizationAggregate.SubscriptionTier? Tier = null
);

public record CreateOrganizationResult(
    Guid OrganizationId
);
