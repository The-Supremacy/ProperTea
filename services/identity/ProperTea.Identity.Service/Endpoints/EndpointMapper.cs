using ProperTea.Identity.Service.Endpoints.Auth;

namespace ProperTea.Identity.Service.Endpoints;

public static class EndpointMapper
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();

        return app;
    }
}