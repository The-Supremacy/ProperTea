using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace ProperTea.User.Features.UserPreferences;

public static class UserPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapUserPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/preferences")
            .RequireAuthorization()
            .WithTags("User Preferences");

        _ = group.MapGet("/", GetPreferences)
            .WithName("GetUserPreferences")
            .Produces<GetUserPreferencesResponse>();

        _ = group.MapPut("/", UpdatePreferences)
            .WithName("UpdateUserPreferences")
            .Produces(204)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> GetPreferences(
        HttpContext context,
        [FromServices] IMessageBus bus,
        CancellationToken ct)
    {
        var externalUserId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var query = new GetUserPreferencesQuery(externalUserId);
        var result = await bus.InvokeAsync<GetUserPreferencesResponse?>(query, ct);

        if (result == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> UpdatePreferences(
        HttpContext context,
        [FromBody] UpdateUserPreferencesRequest request,
        [FromServices] IMessageBus bus,
        CancellationToken ct)
    {
        var externalUserId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateUserPreferencesCommand(
            externalUserId,
            request.Theme,
            request.Language
        );

        await bus.InvokeAsync(command, ct);

        return Results.NoContent();
    }
}

public record GetUserPreferencesResponse(string Theme, string Language);

public record UpdateUserPreferencesRequest(string Theme, string Language);

public record GetUserPreferencesQuery(string ExternalUserId);

public record UpdateUserPreferencesCommand(string ExternalUserId, string Theme, string Language);
