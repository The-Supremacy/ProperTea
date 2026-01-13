using Microsoft.AspNetCore.Mvc;

namespace ProperTea.Landlord.Bff.Organizations;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations")
            .WithTags("Organizations")
            .RequireAuthorization();

        _ = group.MapGet("/{id:guid}", GetOrganization)
            .WithName("GetOrganization");

        _ = group.MapGet("/context", GetOrganizationContext)
            .WithName("GetOrganizationContext");

        _ = group.MapGet("/{id:guid}/audit-log", GetAuditLog)
            .WithName("GetOrganizationAuditLog");

        _ = group.MapGet("/check-availability", CheckAvailability)
            .WithName("CheckAvailability");

        _ = group.MapPost("/", CreateOrganization)
            .WithName("CreateOrganization");

        _ = group.MapPatch("/{id:guid}", UpdateOrganization)
            .WithName("UpdateOrganization");

        _ = group.MapPost("/{id:guid}/deactivate", DeactivateOrganization)
            .WithName("DeactivateOrganization");

        return endpoints;
    }

    private static async Task<IResult> GetOrganization(
        Guid id,
        OrganizationClient client,
        CancellationToken ct)
    {
        var org = await client.GetOrganizationAsync(id, ct);
        return org is null ? Results.NotFound() : Results.Ok(org);
    }

    private static async Task<IResult> GetOrganizationContext(
        OrganizationClient client,
        CancellationToken ct)
    {
        var context = await client.GetOrganizationContextAsync(ct);
        return Results.Ok(context);
    }

    private static async Task<IResult> GetAuditLog(
        Guid id,
        OrganizationClient client,
        CancellationToken ct)
    {
        var auditLog = await client.GetAuditLogAsync(id, ct);
        return auditLog is null ? Results.NotFound() : Results.Ok(auditLog);
    }

    private static async Task<IResult> CheckAvailability(
        [FromQuery] string? name,
        [FromQuery] string? slug,
        OrganizationClient client,
        CancellationToken ct)
    {
        var result = await client.CheckAvailabilityAsync(name, slug, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        OrganizationClient client,
        CancellationToken ct)
    {
        var result = await client.CreateOrganizationAsync(request, ct);
        return Results.Created($"/api/organizations/{result.OrganizationId}", result);
    }

    private static async Task<IResult> UpdateOrganization(
        Guid id,
        UpdateOrganizationRequest request,
        OrganizationClient client,
        CancellationToken ct)
    {
        await client.UpdateOrganizationAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateOrganization(
        Guid id,
        DeactivateOrganizationRequest request,
        OrganizationClient client,
        CancellationToken ct)
    {
        await client.DeactivateOrganizationAsync(id, request, ct);
        return Results.NoContent();
    }
}
