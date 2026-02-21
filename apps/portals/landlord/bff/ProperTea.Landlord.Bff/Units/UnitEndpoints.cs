using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Units;

public static class UnitEndpoints
{
    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/units")
            .RequireAuthorization()
            .WithTags("Units");

        _ = group.MapGet("", GetAllUnits)
            .WithName("GetAllUnits");

        _ = group.MapGet("/{id:guid}", GetUnit)
            .WithName("GetUnit");

        _ = group.MapPost("", CreateUnit)
            .WithName("CreateUnit");

        _ = group.MapPut("/{id:guid}", UpdateUnit)
            .WithName("UpdateUnit");

        _ = group.MapDelete("/{id:guid}", DeleteUnit)
            .WithName("DeleteUnit");

        _ = group.MapGet("/{id:guid}/audit-log", GetUnitAuditLog)
            .WithName("GetUnitAuditLog");

        var propertyGroup = app.MapGroup("/api/properties/{propertyId:guid}/units")
            .RequireAuthorization()
            .WithTags("Units");

        _ = propertyGroup.MapGet("", GetUnitsForProperty)
            .WithName("GetUnitsForProperty");

        _ = propertyGroup.MapGet("/select", SelectUnits)
            .WithName("SelectUnits");

        return app;
    }

    private static async Task<IResult> GetAllUnits(
        [FromServices] UnitClient client,
        [AsParameters] ListUnitsQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var units = await client.GetUnitsAsync(null, query, pagination, sort, ct);
        return Results.Ok(units);
    }

    private static async Task<IResult> GetUnitsForProperty(
        Guid propertyId,
        [FromServices] UnitClient client,
        [AsParameters] ListUnitsQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var units = await client.GetUnitsAsync(propertyId, query, pagination, sort, ct);
        return Results.Ok(units);
    }

    private static async Task<IResult> SelectUnits(
        Guid propertyId,
        [FromServices] UnitClient client,
        CancellationToken ct = default)
    {
        var units = await client.SelectUnitsAsync(propertyId, ct);
        return Results.Ok(units);
    }

    private static async Task<IResult> GetUnit(
        Guid id,
        [FromServices] UnitClient client,
        CancellationToken ct)
    {
        var unit = await client.GetUnitAsync(id, ct);
        return unit is null ? Results.NotFound() : Results.Ok(unit);
    }

    private static async Task<IResult> CreateUnit(
        [FromBody] CreateUnitRequest request,
        [FromServices] UnitClient client,
        CancellationToken ct)
    {
        var unit = await client.CreateUnitAsync(request, ct);
        return Results.Created("/api/units", unit);
    }

    private static async Task<IResult> UpdateUnit(
        Guid id,
        [FromBody] UpdateUnitRequest request,
        [FromServices] UnitClient client,
        CancellationToken ct)
    {
        await client.UpdateUnitAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteUnit(
        Guid id,
        [FromServices] UnitClient client,
        CancellationToken ct)
    {
        await client.DeleteUnitAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetUnitAuditLog(
        Guid id,
        [FromServices] UnitClient client,
        CancellationToken ct)
    {
        var log = await client.GetUnitAuditLogAsync(id, ct);
        return Results.Ok(log);
    }
}
