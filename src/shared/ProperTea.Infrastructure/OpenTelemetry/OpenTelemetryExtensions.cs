using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProperTea.Infrastructure.OpenTelemetry;

public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder AddOpenTelemetry(
        this IHostApplicationBuilder builder,
        OpenTelemetryOptions options)
    {
        var appName = builder.Environment.ApplicationName;
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(appName);

        builder.AddLogging(options, resourceBuilder);

        builder.Services
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
    
    private static void ConfigureTracing(TracerProviderBuilder tracing, string appName, IConfiguration configuration, OpenTelemetryOptions options)
    {
        tracing.AddSource(appName)
            .SetErrorStatusOnException()
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
            .AddHttpClientInstrumentation();
        
        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
            });
        }
        
        var azureMonitorConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(azureMonitorConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = azureMonitorConnectionString);
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, IConfiguration configuration, OpenTelemetryOptions options)
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
        
        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            metrics.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
            });
        }
        
        var azureMonitorConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(azureMonitorConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = azureMonitorConnectionString);
        }
    }

    private static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder,
        OpenTelemetryOptions options, ResourceBuilder resourceBuilder)
    {
        if (!options.LoggingEnabled)
            return builder;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return builder;
    }

    public static TBuilder AddProperHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapProperTelemetryEndpoints(this WebApplication app)
    {
        const string healthEndpointPath = "/health";
        const string alivenessEndpointPath = "/alive";
        
        app.MapHealthChecks(healthEndpointPath);
        app.MapHealthChecks(alivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}