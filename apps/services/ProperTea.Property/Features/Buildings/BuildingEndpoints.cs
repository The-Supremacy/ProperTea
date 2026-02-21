using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Auth;
using ProperTea.Infrastructure.Common.Pagination;
using ProperTea.Property.Features.Buildings.Lifecycle;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Property.Features.Buildings;

public static class BuildingEndpoints
{
    [WolverineGet("/buildings")]
    [Authorize]
    public static async Task<IResult> ListAllBuildings(
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] SortQuery sort,
        [FromQuery] Guid? propertyId = null,
        [FromQuery] string? code = null,
        [FromQuery] string? name = null)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<PagedResult<BuildingListItemResponse>>(
            tenantId,
            new ListBuildings(
                new BuildingFilters { PropertyId = propertyId, Code = code, Name = name },
                pagination,
                sort));

        return Results.Ok(result);
    }

    [WolverineGet("/properties/{propertyId}/buildings")]
    [Authorize]
    public static async Task<IResult> ListBuildings(
        Guid propertyId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] SortQuery sort,
        [FromQuery] string? code = null,
        [FromQuery] string? name = null)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<PagedResult<BuildingListItemResponse>>(
            tenantId,
            new ListBuildings(
                new BuildingFilters { PropertyId = propertyId, Code = code, Name = name },
                pagination,
                sort));

        return Results.Ok(result);
    }

    [WolverineGet("/properties/{propertyId}/buildings/select")]
    [Authorize]
    public static async Task<IResult> SelectBuildings(
        Guid propertyId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<List<BuildingSelectItem>>(
            tenantId,
            new SelectBuildings(propertyId));

        return Results.Ok(result);
    }

    [WolverinePost("/properties/{propertyId}/buildings")]
    [Authorize]
    public static async Task<IResult> CreateBuilding(
        Guid propertyId,
        BuildingCreateRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var buildingId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new CreateBuilding(propertyId, request.Code, request.Name, request.Address));

        return Results.Created($"/properties/{propertyId}/buildings/{buildingId}", new { Id = buildingId });
    }

    [WolverineGet("/buildings/{id}")]
    [Authorize]
    public static async Task<IResult> GetBuilding(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<BuildingResponse?>(tenantId, new GetBuilding(id));
        return result == null ? Results.NotFound() : Results.Ok(result);
    }

    [WolverinePut("/buildings/{id}")]
    [Authorize]
    public static async Task<IResult> UpdateBuilding(
        Guid id,
        BuildingUpdateRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateBuilding(id, request.Code, request.Name, request.Address));

        return Results.NoContent();
    }

    [WolverineDelete("/buildings/{id}")]
    [Authorize]
    public static async Task<IResult> DeleteBuilding(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(tenantId, new DeleteBuilding(id));
        return Results.NoContent();
    }

    [WolverineGet("/buildings/{id}/audit-log")]
    [Authorize]
    public static async Task<IResult> GetBuildingAuditLog(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<BuildingAuditLogResponse>(
            tenantId,
            new GetBuildingAuditLogQuery(id));

        return Results.Ok(result);
    }

    [WolverinePost("/buildings/{id}/entrances")]
    [Authorize]
    public static async Task<IResult> AddEntrance(
        Guid id,
        EntranceWriteRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var entranceId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new AddEntrance(id, request.Code, request.Name));

        return Results.Created($"/buildings/{id}/entrances/{entranceId}", new { Id = entranceId });
    }

    [WolverinePut("/buildings/{id}/entrances/{entranceId}")]
    [Authorize]
    public static async Task<IResult> UpdateEntrance(
        Guid id,
        Guid entranceId,
        EntranceWriteRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateEntrance(id, entranceId, request.Code, request.Name));

        return Results.NoContent();
    }

    [WolverineDelete("/buildings/{id}/entrances/{entranceId}")]
    [Authorize]
    public static async Task<IResult> RemoveEntrance(
        Guid id,
        Guid entranceId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new RemoveEntrance(id, entranceId));

        return Results.NoContent();
    }
}

public record BuildingCreateRequest(string Code, string Name, AddressRequest? Address);

public record BuildingUpdateRequest(string? Code, string? Name, AddressRequest? Address);

public record EntranceWriteRequest(string Code, string Name);
