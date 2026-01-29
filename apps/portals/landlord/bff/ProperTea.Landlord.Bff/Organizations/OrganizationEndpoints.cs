using Microsoft.AspNetCore.Mvc;

namespace ProperTea.Landlord.Bff.Organizations;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations")
            .WithTags("Organizations");

        // Anonymous endpoints for registration flow
        _ = group.MapGet("/check-availability", CheckAvailability)
            .WithName("CheckAvailability")
            .AllowAnonymous();

        _ = group.MapPost("/", RegisterOrganization)
            .WithName("RegisterOrganization")
            .AllowAnonymous();

        // Authenticated endpoints
        _ = group.MapGet("/{id:guid}/audit-log", GetAuditLog)
            .WithName("GetOrganizationAuditLog")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterOrganization(
        RegisterOrganizationRequest request,
        OrganizationClientAnonymous client,
        CancellationToken ct)
    {
        var result = await client.RegisterOrganizationAsync(request, ct);
        return Results.Created($"/api/organizations/{result.OrganizationId}", result);
    }

    private static async Task<IResult> CheckAvailability(
        [FromQuery] string? name,
        OrganizationClientAnonymous client,
        CancellationToken ct)
    {
        var result = await client.CheckAvailabilityAsync(name, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAuditLog(
        Guid id,
        OrganizationClient client,
        CancellationToken ct)
    {
        var auditLog = await client.GetAuditLogAsync(id, ct);
        return auditLog is null ? Results.NotFound() : Results.Ok(auditLog);
    }
}
