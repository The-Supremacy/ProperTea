using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Auth;
using ProperTea.Infrastructure.Common.Pagination;
using ProperTea.Property.Features.Units.Lifecycle;
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
                request.EntranceId,
                request.Code,
                request.Category,
                request.Address,
                request.Floor));

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
                request.PropertyId,
                request.BuildingId,
                request.EntranceId,
                request.Code,
                request.Category,
                request.Address,
                request.Floor));

        return Results.NoContent();
    }

    [WolverineGet("/units/{id}/audit-log")]
    [Authorize]
    public static async Task<IResult> GetUnitAuditLog(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<UnitAuditLogResponse>(
            tenantId,
            new GetUnitAuditLog(id));

        return Results.Ok(result);
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
    Guid? EntranceId,
    string Code,
    UnitCategory Category,
    AddressRequest Address,
    int? Floor);

public record UpdateUnitRequest(
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    UnitCategory Category,
    AddressRequest Address,
    int? Floor);
