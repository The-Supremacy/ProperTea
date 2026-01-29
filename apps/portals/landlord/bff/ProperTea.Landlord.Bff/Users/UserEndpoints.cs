using Microsoft.AspNetCore.Mvc;

namespace ProperTea.Landlord.Bff.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        _ = group.MapGet("/me", GetMyProfile)
            .WithName("GetMyProfile");

        _ = group.MapGet("/preferences", GetPreferences)
            .WithName("GetUserPreferences");

        _ = group.MapPut("/preferences", UpdatePreferences)
            .WithName("UpdateUserPreferences");

        return endpoints;
    }

    private static async Task<IResult> GetMyProfile(
        UserClient client,
        CancellationToken ct)
    {
        var profile = await client.GetMyProfileAsync(ct);
        return Results.Ok(profile);
    }

    private static async Task<IResult> GetPreferences(
        UserClient client,
        CancellationToken ct)
    {
        var preferences = await client.GetPreferencesAsync(ct);

        if (preferences == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(preferences);
    }

    private static async Task<IResult> UpdatePreferences(
        [FromBody] UpdateUserPreferencesRequest request,
        UserClient client,
        CancellationToken ct)
    {
        await client.UpdatePreferencesAsync(request, ct);
        return Results.NoContent();
    }
}
