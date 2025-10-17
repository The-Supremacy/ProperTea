using ProperTea.Identity.Service.Endpoints.Auth;
using ProperTea.Identity.Service.Endpoints.Token;

namespace ProperTea.Identity.Service.Endpoints;

public static class EndpointMapper
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapTokenEndpoints();

        return app;
    }
}