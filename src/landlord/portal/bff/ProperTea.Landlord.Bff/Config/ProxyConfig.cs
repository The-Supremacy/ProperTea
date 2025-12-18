using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Transforms;

namespace ProperTea.Landlord.Bff.Config;

public static class ProxyConfig
{
    public static IServiceCollection AddBffProxy(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                _ = builderContext.AddRequestTransform(async transformContext =>
                {
                    // 1. Inject Access Token
                    var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        transformContext.ProxyRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    // 2. Inject Organization ID from header sent by FE
                    if (transformContext.HttpContext.Request.Headers.TryGetValue("X-Organization-Id", out var orgId))
                    {
                        transformContext.ProxyRequest.Headers.Add("X-Organization-Id", orgId.ToArray());
                    }
                });
            });

        return services;
    }
}
