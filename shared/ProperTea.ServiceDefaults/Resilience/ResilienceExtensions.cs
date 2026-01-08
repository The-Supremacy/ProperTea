using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProperTea.ServiceDefaults.Resilience;

public static class ResilienceExtensions
{
    public static IHostApplicationBuilder AddHttpResilience(this IHostApplicationBuilder builder)
    {
        _ = builder.Services.ConfigureHttpClientDefaults(http =>
            {
                _ = http.AddStandardResilienceHandler();
            });

        return builder;
    }
}
