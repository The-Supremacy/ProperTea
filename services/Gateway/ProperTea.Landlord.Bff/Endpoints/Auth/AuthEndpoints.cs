namespace ProperTea.Landlord.Bff.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapLoginEndpoint();
        group.MapLogoutEndpoint();

        return app;
    }
}