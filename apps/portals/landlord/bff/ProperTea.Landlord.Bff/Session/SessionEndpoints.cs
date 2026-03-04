using ProperTea.Infrastructure.Common.Auth;

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

    private static IResult GetSession(HttpContext context, IConfiguration configuration)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var authServerUrl = configuration["Keycloak:AuthServerUrl"]?.TrimEnd('/') ?? string.Empty;
        var realm = configuration["Keycloak:Realm"] ?? string.Empty;
        var accountUrl = $"{authServerUrl}/realms/{realm}/account";

        if (!isAuthenticated)
        {
            return Results.Ok(new SessionDto(
                UserId: string.Empty,
                IsAuthenticated: false,
                EmailAddress: string.Empty,
                FirstName: string.Empty,
                LastName: string.Empty,
                OrganizationId: string.Empty,
                OrganizationName: string.Empty,
                AccountUrl: accountUrl,
                LastSeenAt: null
            ));
        }

        var userId = context.User.FindFirst("sub")?.Value ?? string.Empty;
        var email = context.User.FindFirst("email")?.Value ?? string.Empty;
        var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
        var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;
        var (parsedOrgId, parsedOrgName) = OrganizationIdProvider.ParseOrganizationClaim(context.User);
        var organizationId = parsedOrgId ?? string.Empty;
        var orgName = parsedOrgName ?? string.Empty;

        return Results.Ok(new SessionDto(
            UserId: userId,
            IsAuthenticated: true,
            EmailAddress: email,
            FirstName: firstName,
            LastName: lastName,
            OrganizationId: organizationId,
            OrganizationName: orgName,
            AccountUrl: accountUrl,
            LastSeenAt: DateTimeOffset.UtcNow
        ));
    }
}

public record SessionDto(
    string UserId,
    bool IsAuthenticated,
    string EmailAddress,
    string FirstName,
    string LastName,
    string OrganizationId,
    string OrganizationName,
    string AccountUrl,
    DateTimeOffset? LastSeenAt
);
