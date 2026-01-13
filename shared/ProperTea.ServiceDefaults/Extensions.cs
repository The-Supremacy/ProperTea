using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ProperTea.ServiceDefaults.ErrorHandling;
using ProperTea.ServiceDefaults.OpenTelemetry;

namespace ProperTea.ServiceDefaults;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServiceDefaults()
        {
            _ = builder.AddOpenTelemetry();
            _ = builder.AddDefaultHealthChecks();
            _ = builder.AddGlobalErrorHandling();
            _ = builder.Services.AddServiceDiscovery();
            _ = builder.Services.ConfigureHttpClientDefaults(http =>
            {
                _ = http.AddServiceDiscovery();
            });

            return builder;
        }

        public TBuilder AddDefaultHealthChecks()
        {
            _ = builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            _ = app.MapHealthChecks("/health");
            _ = app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
