using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProperTea.ProperTelemetry;

public static class OpenTelemetryExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static IHostApplicationBuilder AddProperTelemetry(
        this IHostApplicationBuilder builder,
        OpenTelemetryOptions options)
    {
        var appName = builder.Environment.ApplicationName;

        builder = builder.AddLogging(options);
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(appName))
            .AddTracing(options, appName)
            .AddMetrics(options)
            .AddOpenTelemetryExporters(builder, options);

        return builder;
    }

    private static OpenTelemetryBuilder AddTracing(
        this OpenTelemetryBuilder builder,
        OpenTelemetryOptions options,
        string appName)
    {
        if (!options.TracingEnabled)
            return builder;

        builder.WithTracing(tracing =>
        {
            tracing.AddSource(appName)
                .SetErrorStatusOnException()
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
                .AddHttpClientInstrumentation();
        });

        return builder;
    }

    private static OpenTelemetryBuilder AddMetrics(this OpenTelemetryBuilder builder, OpenTelemetryOptions options)
    {
        if (!options.MetricsEnabled)
            return builder;

        builder.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        });

        return builder;
    }

    private static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder,
        OpenTelemetryOptions options)
    {
        if (!options.LoggingEnabled)
            return builder;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return builder;
    }

    private static OpenTelemetryBuilder AddOpenTelemetryExporters(
        this OpenTelemetryBuilder builder,
        IHostApplicationBuilder hostBuilder,
        OpenTelemetryOptions options)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(options.OtlpEndpoint);
        if (useOtlpExporter)
            builder.Services.AddOpenTelemetry().UseOtlpExporter(OtlpExportProtocol.HttpProtobuf,
                new Uri(options.OtlpEndpoint));

        var useAzureMonitorExporter =
            !string.IsNullOrEmpty(hostBuilder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
        if (useAzureMonitorExporter)
            builder.Services.AddOpenTelemetry().UseAzureMonitor();

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
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}