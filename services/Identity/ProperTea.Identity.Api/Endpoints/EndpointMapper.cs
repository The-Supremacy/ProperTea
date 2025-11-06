using ProperTea.Identity.Api.Endpoints.Auth;
using ProperTea.Identity.Api.Endpoints.Token;

namespace ProperTea.Identity.Api.Endpoints;

public static class EndpointMapper
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapTokenEndpoints();

        return app;
    }
}