namespace ProperTea.Landlord.Bff.Session;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/session")
            .WithTags("Session");

        _ = group.MapGet("", GetSession)
            .WithName("GetSession")
            .AllowAnonymous();

        return endpoints;
    }

    private static IResult GetSession(HttpContext context)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            return Results.Ok(new SessionDto(
                UserId: Guid.Empty,
                ExternalUserId: string.Empty,
                IsAuthenticated: false,
                EmailAddress: string.Empty,
                FirstName: string.Empty,
                LastName: string.Empty,
                OrganizationId: Guid.Empty,
                OrganizationName: string.Empty,
                LastSeenAt: null
            ));
        }

        var userId = context.User.FindFirst("sub")?.Value ?? string.Empty;
        var email = context.User.FindFirst("email")?.Value ?? string.Empty;
        var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
        var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;
        var orgId = context.User.FindFirst("urn:zitadel:iam:org:id")?.Value ?? string.Empty;
        var orgName = context.User.FindFirst("urn:zitadel:iam:org:domain")?.Value ?? string.Empty;

        return Results.Ok(new SessionDto(
            UserId: Guid.TryParse(userId, out var uid) ? uid : Guid.Empty,
            ExternalUserId: userId,
            IsAuthenticated: true,
            EmailAddress: email,
            FirstName: firstName,
            LastName: lastName,
            OrganizationId: Guid.TryParse(orgId, out var oid) ? oid : Guid.Empty,
            OrganizationName: orgName,
            LastSeenAt: DateTimeOffset.UtcNow
        ));
    }
}

public record SessionDto(
    Guid UserId,
    string ExternalUserId,
    bool IsAuthenticated,
    string EmailAddress,
    string FirstName,
    string LastName,
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset? LastSeenAt
);
