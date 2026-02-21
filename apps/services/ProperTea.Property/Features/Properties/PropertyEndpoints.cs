using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Auth;
using ProperTea.Infrastructure.Common.Pagination;
using ProperTea.Property.Features.Properties.Lifecycle;
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
                request.Address?.ToAddress() ?? new Address(Country.UA, string.Empty, string.Empty, string.Empty)));

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
                request.Address?.ToAddress()));

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

        var result = await bus.InvokeForTenantAsync<PropertyAuditLogResponse>(
            tenantId,
            new GetPropertyAuditLogQuery(id));

        return Results.Ok(result);
    }
}

public record CreatePropertyRequest(
    Guid CompanyId,
    string Code,
    string Name,
    AddressRequest? Address);

public record UpdatePropertyRequest(
    string? Code,
    string? Name,
    AddressRequest? Address);
