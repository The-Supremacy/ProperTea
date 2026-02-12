using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Property.Features.Properties.Buildings;
using ProperTea.Property.Features.Properties.Lifecycle;
using ProperTea.Infrastructure.Common.Auth;
using ProperTea.Infrastructure.Common.Pagination;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.Property.Features.Properties;

public static class PropertyEndpoints
{
    [WolverinePost("/properties")]
    [Authorize]
    public static async Task<IResult> CreateProperty(
        CreatePropertyRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var propertyId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new CreateProperty(
                request.CompanyId,
                request.Code,
                request.Name,
                request.Address,
                request.SquareFootage));

        return Results.Created($"/properties/{propertyId}", new { Id = propertyId });
    }

    [WolverineGet("/properties")]
    [Authorize]
    public static async Task<IResult> ListProperties(
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] PaginationQuery pagination,
        [FromQuery] SortQuery sort,
        [FromQuery] PropertyFilters filters)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<PagedResult<PropertyListItemResponse>>(
            tenantId,
            new ListProperties(filters, pagination, sort));

        return Results.Ok(result);
    }

    [WolverineGet("/properties/select")]
    [Authorize]
    public static async Task<IResult> SelectProperties(
        IMessageBus bus,
        IOrganizationIdProvider orgProvider,
        [FromQuery] Guid? companyId = null)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<List<SelectItem>>(
            tenantId,
            new SelectProperties(companyId));

        return Results.Ok(result);
    }

    [WolverineGet("/properties/{id}")]
    [Authorize]
    public static async Task<IResult> GetProperty(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var property = await bus.InvokeForTenantAsync<PropertyResponse?>(
            tenantId,
            new GetProperty(id));

        return property == null ? Results.NotFound() : Results.Ok(property);
    }

    [WolverinePut("/properties/{id}")]
    [Authorize]
    public static async Task<IResult> UpdateProperty(
        Guid id,
        UpdatePropertyRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateProperty(
                id,
                request.Code,
                request.Name,
                request.Address,
                request.SquareFootage));

        return Results.NoContent();
    }

    [WolverineDelete("/properties/{id}")]
    [Authorize]
    public static async Task<IResult> DeleteProperty(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new DeleteProperty(id));

        return Results.NoContent();
    }

    [WolverineGet("/properties/{id}/audit-log")]
    [Authorize]
    public static async Task<IResult> GetPropertyAuditLog(
        Guid id,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<List<PropertyAuditLogEntry>>(
            tenantId,
            new GetPropertyAuditLog(id));

        return Results.Ok(result);
    }


    [WolverineGet("/properties/{propertyId}/buildings")]
    [Authorize]
    public static async Task<IResult> ListBuildings(
        Guid propertyId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var result = await bus.InvokeForTenantAsync<List<BuildingResponse>>(
            tenantId,
            new ListBuildings(propertyId));

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
    public static async Task<IResult> AddBuilding(
        Guid propertyId,
        BuildingRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var buildingId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new AddBuilding(propertyId, request.Code, request.Name));

        return Results.Created($"/properties/{propertyId}/buildings/{buildingId}", new { Id = buildingId });
    }

    [WolverinePut("/properties/{propertyId}/buildings/{buildingId}")]
    [Authorize]
    public static async Task<IResult> UpdateBuilding(
        Guid propertyId,
        Guid buildingId,
        BuildingRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new UpdateBuilding(propertyId, buildingId, request.Code, request.Name));

        return Results.NoContent();
    }

    [WolverineDelete("/properties/{propertyId}/buildings/{buildingId}")]
    [Authorize]
    public static async Task<IResult> RemoveBuilding(
        Guid propertyId,
        Guid buildingId,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        await bus.InvokeForTenantAsync(
            tenantId,
            new RemoveBuilding(propertyId, buildingId));

        return Results.NoContent();
    }
}

public record CreatePropertyRequest(
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public record UpdatePropertyRequest(
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public record BuildingRequest(
    string Code,
    string Name);
