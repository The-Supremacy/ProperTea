namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth");
        
        group.MapRegisterEndpoint();
        group.MapLoginEndpoint();
        group.MapReissueEndpoint();
        group.MapExternalLoginEndpoint();
        group.MapExternalCallbackEndpoint();
        group.MapForgotPasswordEndpoint();
        group.MapResetPasswordEndpoint();
        group.MapChangePasswordEndpoint();

        return app;
    }
}