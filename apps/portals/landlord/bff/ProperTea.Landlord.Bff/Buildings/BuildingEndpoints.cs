using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Buildings;

public static class BuildingEndpoints
{
    public static IEndpointRouteBuilder MapBuildingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/buildings")
            .RequireAuthorization()
            .WithTags("Buildings");

        _ = group.MapGet("", GetAllBuildings)
            .WithName("GetAllBuildings");

        _ = group.MapGet("/{id:guid}", GetBuilding)
            .WithName("GetBuilding");

        _ = group.MapPut("/{id:guid}", UpdateBuilding)
            .WithName("UpdateBuilding");

        _ = group.MapDelete("/{id:guid}", DeleteBuilding)
            .WithName("DeleteBuilding");

        _ = group.MapGet("/{id:guid}/audit-log", GetBuildingAuditLog)
            .WithName("GetBuildingAuditLog");

        _ = group.MapGet("/property/{propertyId:guid}", GetBuildings)
            .WithName("GetBuildings");

        _ = group.MapGet("/property/{propertyId:guid}/select", SelectBuildings)
            .WithName("SelectBuildings");

        _ = group.MapPost("/property/{propertyId:guid}", CreateBuilding)
            .WithName("CreateBuilding");

        return app;
    }

    private static async Task<IResult> GetAllBuildings(
        [FromServices] BuildingClient client,
        [AsParameters] ListBuildingsQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var buildings = await client.GetBuildingsAsync(query.PropertyId, query, pagination, sort, ct);
        return Results.Ok(buildings);
    }

    private static async Task<IResult> GetBuildings(
        Guid propertyId,
        [FromServices] BuildingClient client,
        [AsParameters] ListBuildingsQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var buildings = await client.GetBuildingsAsync(propertyId, query, pagination, sort, ct);
        return Results.Ok(buildings);
    }

    private static async Task<IResult> SelectBuildings(
        Guid propertyId,
        [FromServices] BuildingClient client,
        CancellationToken ct = default)
    {
        var buildings = await client.SelectBuildingsAsync(propertyId, ct);
        return Results.Ok(buildings);
    }

    private static async Task<IResult> GetBuilding(
        Guid id,
        [FromServices] BuildingClient client,
        CancellationToken ct)
    {
        var building = await client.GetBuildingAsync(id, ct);
        return building is null ? Results.NotFound() : Results.Ok(building);
    }

    private static async Task<IResult> CreateBuilding(
        Guid propertyId,
        [FromBody] CreateBuildingRequest request,
        [FromServices] BuildingClient client,
        CancellationToken ct)
    {
        var building = await client.CreateBuildingAsync(propertyId, request, ct);
        return Results.Created($"/api/buildings", building);
    }

    private static async Task<IResult> UpdateBuilding(
        Guid id,
        [FromBody] UpdateBuildingRequest request,
        [FromServices] BuildingClient client,
        CancellationToken ct)
    {
        await client.UpdateBuildingAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteBuilding(
        Guid id,
        [FromServices] BuildingClient client,
        CancellationToken ct)
    {
        await client.DeleteBuildingAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetBuildingAuditLog(
        Guid id,
        [FromServices] BuildingClient client,
        CancellationToken ct)
    {
        var auditLog = await client.GetBuildingAuditLogAsync(id, ct);
        return Results.Ok(auditLog);
    }
}
