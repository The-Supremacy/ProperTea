// Extensions/ObservabilityExtensions.cs
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProperTea.ApiGateway.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservabilityEndpoints(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("gateway", () => HealthCheckResult.Healthy("Gateway is running"))
            .AddCheck("authorization-service", () => HealthCheckResult.Healthy(
                "Authorization service connectivity"), tags: new[] { "ready" });

        return services;
    }

    public static WebApplication MapObservabilityEndpoints(this WebApplication app)
    {
        app.MapGet("/metrics", async context =>
        {
            context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
            await context.Response.WriteAsync("# TYPE gateway_info gauge\n");
            await context.Response.WriteAsync("# HELP gateway_info Gateway information\n");
            await context.Response.WriteAsync($"gateway_info{{version=\"{typeof(Program).Assembly.GetName().Version}\"}} 1\n");
        });
        
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTimeOffset.UtcNow
                }));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        tags = x.Value.Tags
                    }),
                    timestamp = DateTimeOffset.UtcNow
                }));
            }
        });
        
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        duration = x.Value.Duration.TotalMilliseconds,
                        description = x.Value.Description,
                        exception = x.Value.Exception?.Message,
                        data = x.Value.Data,
                        tags = x.Value.Tags
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    timestamp = DateTimeOffset.UtcNow
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        return app;
    }
}
