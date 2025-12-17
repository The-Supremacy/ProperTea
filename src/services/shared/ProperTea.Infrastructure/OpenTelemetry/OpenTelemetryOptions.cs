namespace ProperTea.Infrastructure.OpenTelemetry;

public record OpenTelemetryOptions(
    bool LoggingEnabled = false,
    bool MetricsEnabled = false,
    bool TracingEnabled = false,
    string OtlpEndpoint = "")
{
}
