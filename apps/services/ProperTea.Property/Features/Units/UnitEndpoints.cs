using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Property.Features.Units.Lifecycle;
using ProperTea.Infrastructure.Common.Auth;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Property.Features.Units;

public static class UnitEndpoints
{
    [WolverinePost("/units")]
    [Authorize]
    public static async Task<IResult> CreateUnit(
        CreateUnitRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var unitId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new CreateUnit(
                request.PropertyId,
                request.BuildingId,
                request.Code,
                request.UnitNumber,
                request.Category,
                request.Floor,
                request.SquareFootage,
                request.RoomCount));

        return Results.Created($"/units/{unitId}", new { Id = unitId });
    }

    [WolverineGet("/units")]
    [Authorize]
    public static async Task<IResult> ListUnits(
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] SortQuery sort,
        [FromQuery] UnitFilters filters)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<PagedResult<UnitListItemResponse>>(
            tenantId,
            new ListUnits(filters, pagination, sort));

        return Results.Ok(result);
    }

    [WolverineGet("/units/{id}")]
    [Authorize]
    public static async Task<IResult> GetUnit(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var unit = await bus.InvokeForTenantAsync<UnitResponse?>(
            tenantId,
            new GetUnit(id));

        return unit == null ? Results.NotFound() : Results.Ok(unit);
    }

    [WolverinePut("/units/{id}")]
    [Authorize]
    public static async Task<IResult> UpdateUnit(
        Guid id,
        UpdateUnitRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateUnit(
                id,
                request.BuildingId,
                request.Code,
                request.UnitNumber,
                request.Category,
                request.Floor,
                request.SquareFootage,
                request.RoomCount));

        return Results.NoContent();
    }

    [WolverineDelete("/units/{id}")]
    [Authorize]
    public static async Task<IResult> DeleteUnit(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new DeleteUnit(id));

        return Results.NoContent();
    }

    [WolverineGet("/properties/{propertyId}/units")]
    [Authorize]
    public static async Task<IResult> ListUnitsForProperty(
        Guid propertyId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] SortQuery sort)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var filters = new UnitFilters { PropertyId = propertyId };
        var result = await bus.InvokeForTenantAsync<PagedResult<UnitListItemResponse>>(
            tenantId,
            new ListUnits(filters, pagination, sort));

        return Results.Ok(result);
    }
}

public record CreateUnitRequest(
    Guid PropertyId,
    Guid? BuildingId,
    string Code,
    string UnitNumber,
    UnitCategory Category,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount);

public record UpdateUnitRequest(
    Guid? BuildingId,
    string Code,
    string UnitNumber,
    UnitCategory Category,
    int? Floor,
    decimal? SquareFootage,
    int? RoomCount);
