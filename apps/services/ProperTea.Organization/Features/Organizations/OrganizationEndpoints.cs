using ProperTea.Organization.Features.Organizations.CheckAvailability;
using ProperTea.Organization.Features.Organizations.Deactivate;
using ProperTea.Organization.Features.Organizations.GetAuditLog;
using ProperTea.Organization.Features.Organizations.GetMyOrganization;
using ProperTea.Organization.Features.Organizations.GetOrganization;
using ProperTea.Organization.Features.Organizations.UpdateIdentity;
using ProperTea.Organization.Features.Organizations.VerifyDomain;
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

        _ = group.MapGet("/my-organization", GetMyOrganization)
            .WithName("GetMyOrganization")
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

        _ = group.MapPost("/{id:guid}/verify-domain", VerifyDomain)
            .WithName("VerifyOrganizationDomain")
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

    private static async Task<IResult> GetMyOrganization(
        IMessageBus bus,
        CancellationToken ct)
    {
        var query = new GetMyOrganizationQuery();
        var response = await bus.InvokeAsync<MyOrganizationResponse>(query, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetOrganization(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        var query = new GetOrganizationQuery(id);
        var response = await bus.InvokeAsync<OrganizationResponse>(query, ct);
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
        // Extract user ID from claims (required for adding as org owner)
        var userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var command = new OrganizationMessages.StartRegistration(
            Guid.NewGuid(),
            request.Name,
            request.Slug,
            userId,
            request.EmailDomain
        );

        var result = await bus.InvokeAsync<OrganizationMessages.RegistrationResult>(command);

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
        var command = new UpdateIdentityCommand(id, request.NewName, request.NewSlug, ct);
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

    private static async Task<IResult> VerifyDomain(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new VerifyDomainCommand(id, ct);
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }
}

public record UpdateIdentityRequest(string? NewName, string? NewSlug);

public record DeactivateRequest(string Reason);

public record CreateOrganizationRequest(
    string Name,
    string Slug,
    string? EmailDomain = null,
    OrganizationAggregate.SubscriptionTier? Tier = null
);

public record CreateOrganizationResult(
    Guid OrganizationId
);
