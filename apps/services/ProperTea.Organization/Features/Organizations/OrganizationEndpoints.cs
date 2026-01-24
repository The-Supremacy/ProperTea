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

        _ = group.MapGet("/{id:guid}/audit-log", GetAuditLog)
            .WithName("GetOrganizationAuditLog")
            .RequireAuthorization();

        _ = group.MapGet("/check-availability", CheckAvailability)
            .WithName("CheckAvailability")
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
            request.OrganizationName,
            request.UserEmail,
            request.UserFirstName,
            request.UserLastName,
            request.Slug
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
}

public record CreateOrganizationRequest(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string Slug
);

public record CreateOrganizationResult(
    Guid OrganizationId
);
