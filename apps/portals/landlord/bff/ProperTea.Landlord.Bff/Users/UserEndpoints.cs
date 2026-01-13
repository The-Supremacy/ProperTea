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

        return endpoints;
    }

    private static async Task<IResult> GetMyProfile(
        UserClient client,
        CancellationToken ct)
    {
        var profile = await client.GetMyProfileAsync(ct);
        return Results.Ok(profile);
    }
}
