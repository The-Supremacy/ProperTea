using System.Security.Claims;
using Wolverine;
using Wolverine.Http;

namespace ProperTea.User.Features.UserPreferences;

public static class UserPreferencesEndpoints
{
    [WolverineGet("/users/preferences")]
    public static async Task<IResult> GetPreferences(
        ClaimsPrincipal user,
        IMessageBus bus)
    {
        var externalUserId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var query = new GetUserPreferencesQuery(externalUserId);
        var result = await bus.InvokeAsync<GetUserPreferencesResponse?>(query);

        if (result == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(result);
    }

    [WolverinePut("/users/preferences")]
    public static async Task<IResult> UpdatePreferences(
        ClaimsPrincipal user,
        UpdateUserPreferencesRequest request,
        IMessageBus bus)
    {
        var externalUserId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateUserPreferencesCommand(
            externalUserId,
            request.Theme,
            request.Language
        );

        await bus.InvokeAsync(command);

        return Results.NoContent();
    }
}

public record GetUserPreferencesResponse(string Theme, string Language);

public record UpdateUserPreferencesRequest(string Theme, string Language);

public record GetUserPreferencesQuery(string ExternalUserId);

public record UpdateUserPreferencesCommand(string ExternalUserId, string Theme, string Language);
