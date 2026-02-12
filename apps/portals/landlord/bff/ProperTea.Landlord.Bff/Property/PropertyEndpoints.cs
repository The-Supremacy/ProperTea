using Microsoft.AspNetCore.Mvc;
using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Property;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/properties")
            .RequireAuthorization()
            .WithTags("Properties");

        _ = group.MapPost("/", CreateProperty)
            .WithName("CreateProperty");

        _ = group.MapGet("/", GetProperties)
            .WithName("GetProperties");

        _ = group.MapGet("/select", SelectProperties)
            .WithName("SelectProperties");

        _ = group.MapGet("/{id:guid}", GetProperty)
            .WithName("GetProperty");

        _ = group.MapPut("/{id:guid}", UpdateProperty)
            .WithName("UpdateProperty");

        _ = group.MapDelete("/{id:guid}", DeleteProperty)
            .WithName("DeleteProperty");

        return app;
    }

    private static async Task<IResult> CreateProperty(
        [FromBody] CreatePropertyRequest request,
        [FromServices] PropertyClient client,
        CancellationToken ct)
    {
        var property = await client.CreatePropertyAsync(request, ct);
        return Results.Created($"/api/properties", property);
    }

    private static async Task<IResult> GetProperties(
        [FromServices] PropertyClient client,
        [AsParameters] ListPropertiesQuery query,
        [AsParameters] PaginationQuery pagination,
        [AsParameters] SortQuery sort,
        CancellationToken ct = default)
    {
        var properties = await client.GetPropertiesAsync(query, pagination, sort, ct);
        return Results.Ok(properties);
    }

    private static async Task<IResult> SelectProperties(
        [FromServices] PropertyClient client,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var properties = await client.SelectPropertiesAsync(companyId, ct);
        return Results.Ok(properties);
    }

    private static async Task<IResult> GetProperty(
        Guid id,
        [FromServices] PropertyClient client,
        CancellationToken ct)
    {
        var property = await client.GetPropertyAsync(id, ct);
        return property is null ? Results.NotFound() : Results.Ok(property);
    }

    private static async Task<IResult> UpdateProperty(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        [FromServices] PropertyClient client,
        CancellationToken ct)
    {
        await client.UpdatePropertyAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteProperty(
        Guid id,
        [FromServices] PropertyClient client,
        CancellationToken ct)
    {
        await client.DeletePropertyAsync(id, ct);
        return Results.NoContent();
    }
}
