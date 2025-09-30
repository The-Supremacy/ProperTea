// Configuration/OpenTelemetryConfiguration.cs
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProperTea.Gateway.Configuration;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var resourceBuilder = CreateResourceBuilder(environment);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource = resourceBuilder)
            .WithTracing(tracing => ConfigureTracing(tracing, configuration, environment))
            .WithMetrics(metrics => ConfigureMetrics(metrics, configuration, environment));

        return services;
    }

    public static ILoggingBuilder AddObservabilityLogging(this ILoggingBuilder logging, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var resourceBuilder = CreateResourceBuilder(environment);

        logging.AddOpenTelemetry(otlp =>
        {
            otlp.SetResourceBuilder(resourceBuilder);
            ConfigureLogging(otlp, configuration, environment);
        });

        return logging;
    }

    private static ResourceBuilder CreateResourceBuilder(IWebHostEnvironment environment)
    {
        return ResourceBuilder.CreateDefault()
            .AddService("ProperTea.Gateway", "1.0.0")
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.namespace"] = "ProperTea",
                ["service.instance.id"] = Environment.MachineName,
                ["deployment.environment"] = environment.EnvironmentName,
                ["service.version"] = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
            });
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, IConfiguration configuration, IWebHostEnvironment environment)
    {
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                
                if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                    activity.SetTag("http.client_ip", forwardedFor.FirstOrDefault());
            };
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response.status_code", response.StatusCode);
            };
            options.Filter = httpContext =>
            {
                var path = httpContext.Request.Path.Value;
                return !IsObservabilityEndpoint(path);
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method.Method);
                activity.SetTag("http.url", request.RequestUri?.ToString());
            };
            options.EnrichWithHttpResponseMessage = (activity, response) =>
            {
                activity.SetTag("http.response.status_code", (int)response.StatusCode);
                activity.SetTag("http.response.content_length", response.Content.Headers.ContentLength);
            };
        })
        .AddSource("ProperTea.Gateway")
        .AddSource("Microsoft.AspNetCore")
        .AddSource("System.Net.Http");
        
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];
        if (environment.IsDevelopment() && !string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                options.Headers = configuration["Observability:OtlpHeaders"];
            });
        }
        else
        {
            // TODO: implement in production.
            tracing.AddConsoleExporter();
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, IConfiguration configuration, IWebHostEnvironment environment)
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddAspNetCoreInstrumentation()
               .AddMeter("ProperTea.Gateway")
               .AddMeter("Microsoft.AspNetCore.Hosting")
               .AddMeter("Microsoft.AspNetCore.Server.Kestrel");

        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];
        if (environment.IsDevelopment() && !string.IsNullOrEmpty(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                options.Headers = configuration["Observability:OtlpHeaders"];
            });
        }
        else
        {
            // TODO: implement in production.
            metrics.AddConsoleExporter();
        }
    }

    private static void ConfigureLogging(OpenTelemetryLoggerOptions logging, IConfiguration configuration, IWebHostEnvironment environment)
    {
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.IncludeFormattedMessage = true;

        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];
        if (environment.IsDevelopment() && !string.IsNullOrEmpty(otlpEndpoint))
        {
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                options.Headers = configuration["Observability:OtlpHeaders"];
            });
        }
        else
        {
            // TODO: implement in production.
            logging.AddConsoleExporter();
        }
    }

    private static bool IsObservabilityEndpoint(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.StartsWith("/health") ||
               path.StartsWith("/metrics") ||
               path.StartsWith("/.well-known") ||
               path.StartsWith("/favicon.ico");
    }
}
