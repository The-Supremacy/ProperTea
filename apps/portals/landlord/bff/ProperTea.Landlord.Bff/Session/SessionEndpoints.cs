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
                ExternalUserId: string.Empty,
                IsAuthenticated: false,
                EmailAddress: string.Empty,
                FirstName: string.Empty,
                LastName: string.Empty,
                ExternalOrganizationId: string.Empty,
                OrganizationName: string.Empty,
                LastSeenAt: null
            ));
        }

        var userId = context.User.FindFirst("sub")?.Value ?? string.Empty;
        var email = context.User.FindFirst("email")?.Value ?? string.Empty;
        var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
        var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;
        var externalOrgId = context.User.FindFirst("urn:zitadel:iam:user:resourceowner:id")?.Value ?? string.Empty;
        var orgName = context.User.FindFirst("urn:zitadel:iam:user:resourceowner:name")?.Value ?? string.Empty;

        return Results.Ok(new SessionDto(
            ExternalUserId: userId,
            IsAuthenticated: true,
            EmailAddress: email,
            FirstName: firstName,
            LastName: lastName,
            ExternalOrganizationId: externalOrgId,
            OrganizationName: orgName,
            LastSeenAt: DateTimeOffset.UtcNow
        ));
    }
}

public record SessionDto(
    string ExternalUserId,
    bool IsAuthenticated,
    string EmailAddress,
    string FirstName,
    string LastName,
    string ExternalOrganizationId,
    string OrganizationName,
    DateTimeOffset? LastSeenAt
);
