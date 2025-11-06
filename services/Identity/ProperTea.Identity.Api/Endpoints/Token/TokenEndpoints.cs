namespace ProperTea.Identity.Api.Endpoints.Token;

public static class TokenEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/token");

        group.MapLoginEndpoint();
        group.MapReissueEndpoint();
        group.MapExternalLoginEndpoint();
        group.MapExternalCallbackEndpoint();

        return app;
    }
}