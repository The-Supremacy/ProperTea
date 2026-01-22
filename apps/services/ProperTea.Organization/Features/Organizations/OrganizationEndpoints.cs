using ProperTea.Organization.Features.Organizations.Lifecycle;
using Wolverine;

namespace ProperTea.Organization.Features.Organizations;

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations")
            .WithTags("Organizations");

        _ = group.MapPost("/", RegisterOrganization)
            .WithName("RegisterOrganization")
            .RequireAuthorization();

        _ = group.MapGet("/{id:guid}", GetOrganization)
            .WithName("GetOrganization")
            .RequireAuthorization();

        _ = group.MapGet("/{id:guid}/audit-log", GetAuditLog)
            .WithName("GetOrganizationAuditLog")
            .RequireAuthorization();

        _ = group.MapGet("/check-availability", CheckAvailability)
            .WithName("CheckAvailability")
            .RequireAuthorization();

        _ = group.MapPatch("/{id:guid}", UpdateIdentity)
            .WithName("UpdateOrganizationIdentity")
            .RequireAuthorization();

        _ = group.MapPost("/{id:guid}/deactivate", DeactivateOrganization)
            .WithName("DeactivateOrganization")
            .RequireAuthorization();

        _ = group.MapPost("/{id:guid}/activate", ActivateOrganization)
            .WithName("ActivateOrganization")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> CheckAvailability(
        [AsParameters] CheckAvailabilityQuery query,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<CheckAvailabilityResult>(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOrganization(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        var query = new GetOrganizationQuery(id);
        var response = await bus.InvokeAsync<GetOrganizationResponse>(query, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetAuditLog(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        var query = new GetAuditLogQuery(id);
        var response = await bus.InvokeAsync<AuditLogResponse>(query, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> RegisterOrganization(
        CreateOrganizationRequest request,
        IMessageBus bus,
        HttpContext context)
    {
        var command = new RegisterOrganizationCommand(
            Guid.NewGuid(),
            request.Name,
            request.Slug,
            request.Slug,
            request.Domains
        );

        var result = await bus.InvokeAsync<RegistrationResult>(command);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Organization Registration Failed",
                detail: result.Reason,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return Results.Created($"/organizations/{result.OrganizationId}", new CreateOrganizationResult(
            result.OrganizationId
        ));
    }

    private static async Task<IResult> UpdateIdentity(
        Guid id,
        UpdateIdentityRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new UpdateIdentityCommand(id, request.NewName, request.NewSlug, request.UpdatedDomains, ct);
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateOrganization(
        Guid id,
        DeactivateRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new DeactivateCommand(id, request.Reason, ct);
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateOrganization(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new ActivateCommand(id, ct);
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }
}

public record UpdateIdentityRequest(string? NewName, string? NewSlug, List<string> UpdatedDomains);

public record DeactivateRequest(string Reason);

public record CreateOrganizationRequest(
    string Name,
    string Slug,
    List<string> Domains,
    OrganizationAggregate.SubscriptionTier? Tier = null
);

public record CreateOrganizationResult(
    Guid OrganizationId
);
