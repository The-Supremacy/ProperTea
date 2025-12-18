using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProperTea.Infrastructure.OpenTelemetry;

public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder AddProperOpenTelemetry(
        this IHostApplicationBuilder builder,
        OpenTelemetryOptions options)
    {
        var appName = builder.Environment.ApplicationName;
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(appName);

        _ = builder.AddLogging(options, resourceBuilder);

        _ = builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                if (!options.TracingEnabled)
                    return;

                ConfigureTracing(tracing, appName, builder.Configuration, options);
            })
            .WithMetrics(metrics =>
            {
                if (!options.MetricsEnabled)
                    return;

                ConfigureMetrics(metrics, builder.Configuration, options);
            });

        return builder;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, string appName, IConfiguration configuration,
        OpenTelemetryOptions options)
    {
        _ = tracing.AddSource(appName)
            .SetErrorStatusOnException()
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
            .AddHttpClientInstrumentation()
            .AddSource("Wolverine");

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            _ = tracing.AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = new Uri(options.OtlpEndpoint); });
        }

        var azureMonitorConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(azureMonitorConnectionString))
        {
            _ = tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = azureMonitorConnectionString);
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, IConfiguration configuration,
        OpenTelemetryOptions options)
    {
        _ = metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("Wolverine");

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            _ = metrics.AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = new Uri(options.OtlpEndpoint); });
        }

        var azureMonitorConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(azureMonitorConnectionString))
        {
            _ = metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = azureMonitorConnectionString);
        }
    }

    private static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder,
        OpenTelemetryOptions options, ResourceBuilder resourceBuilder)
    {
        if (!options.LoggingEnabled)
            return builder;

        _ = builder.Services.AddLogging();
        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            _ = logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;

            if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            {
                _ = logging.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
                });
            }
        });

        return builder;
    }

    public static TBuilder AddProperHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapTelemetryEndpoints(this WebApplication app)
    {
        const string healthEndpointPath = "/health";
        const string alivenessEndpointPath = "/alive";

        _ = app.MapHealthChecks(healthEndpointPath);
        _ = app.MapHealthChecks(alivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
