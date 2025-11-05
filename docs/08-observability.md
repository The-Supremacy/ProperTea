# Observability Strategy

**Version:** 1.0.0  
**Last Updated:** October 25, 2025  
**Status:** MVP 1 Configuration Guide

---

## Table of Contents

1. [Overview](#overview)
2. [The Three Pillars](#the-three-pillars)
3. [OpenTelemetry Configuration](#opentelemetry-configuration)
4. [Distributed Tracing](#distributed-tracing)
5. [Logging](#logging)
6. [Metrics](#metrics)
7. [Dashboards & Alerting](#dashboards--alerting)
8. [Local vs Production Setup](#local-vs-production-setup)

---

## Overview

ProperTea implements comprehensive observability using **OpenTelemetry** as the instrumentation layer, with different
backends for local development and production.

### Observability Goals

- **Fast Problem Detection** - Know when something breaks within seconds
- **Root Cause Analysis** - Trace issues across service boundaries
- **Performance Monitoring** - Identify slow queries, high latency endpoints
- **Capacity Planning** - Understand resource usage trends
- **Business Insights** - Track user journeys, conversion rates

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    All Services                              │
│  (Identity, Contact, Property, BFFs, etc.)                  │
│                                                              │
│  OpenTelemetry SDK                                          │
│  - Traces (HTTP, DB, events)                                │
│  - Metrics (request count, latency, custom)                 │
│  - Logs (structured JSON)                                    │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────▼──────────────┐
        │  OTel Collector (Local)   │
        │  - Receives telemetry     │
        │  - Routes to backends     │
        └────────────┬──────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
    ┌────▼────┐ ┌───▼────┐ ┌───▼──────┐
    │ Jaeger  │ │ Loki   │ │Prometheus│
    │(Traces) │ │(Logs)  │ │(Metrics) │
    └────┬────┘ └───┬────┘ └───┬──────┘
         │          │           │
         └──────────┼───────────┘
                    │
              ┌─────▼──────┐
              │  Grafana   │
              │ (Dashboards)│
              └────────────┘
```

**Production (Azure):**

```
All Services → OTel SDK → Azure Monitor / Application Insights
```

---

## The Three Pillars

### 1. Traces (What happened?)

**Distributed traces** show request flow across services:

```
User Registration Request
├─ BFF: POST /api/auth/register (120ms)
│  └─ Identity Service: POST /api/auth/register (80ms)
│     ├─ Database: INSERT user (15ms)
│     └─ Kafka: Publish UserCreated (5ms)
└─ Contact Worker: Process UserCreated (45ms)
   └─ Database: INSERT contact (12ms)
```

### 2. Logs (What was the system doing?)

**Structured logs** provide context:

```json
{
  "timestamp": "2025-10-22T10:15:30Z",
  "level": "Information",
  "message": "User registered successfully",
  "userId": "guid",
  "email": "alice@example.com",
  "traceId": "abc123",
  "spanId": "def456",
  "service": "Identity.Service"
}
```

### 3. Metrics (How is the system performing?)

**Time-series metrics** show trends:

- HTTP request rate: 120 req/s
- Average latency: 45ms (p95: 120ms, p99: 250ms)
- Error rate: 0.5%
- Database connection pool: 15/20 connections used

---

## OpenTelemetry Configuration

### ProperTea.ProperTelemetry Library

**Shared configuration for all services:**

```csharp
// ProperTea.ProperTelemetry/ServiceCollectionExtensions.cs
public static IHostApplicationBuilder AddProperTelemetry(
    this IHostApplicationBuilder builder,
    OpenTelemetryOptions options)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(options.ServiceName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"
            })
        )
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = httpContext => 
                    !httpContext.Request.Path.StartsWithSegments("/health"); // Don't trace health checks
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
                opts.SetDbStatementForStoredProcedure = true;
            })
            .AddSource(options.ServiceName)
            .AddOtlpExporter(opts => opts.Endpoint = new Uri(options.Endpoint))
        )
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(options.ServiceName)
            .AddOtlpExporter(opts => opts.Endpoint = new Uri(options.Endpoint))
        );

    // Logging
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter(opts => opts.Endpoint = new Uri(options.Endpoint));
    });

    return builder;
}
```

### Service Configuration

```json
// appsettings.Development.json
{
  "OpenTelemetry": {
    "ServiceName": "ProperTea.Identity.Service",
    "Endpoint": "http://jaeger:4317",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true
  }
}

// appsettings.Production.json
{
  "OpenTelemetry": {
    "ServiceName": "ProperTea.Identity.Service",
    "Endpoint": "https://your-app-insights.applicationinsights.azure.com",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true
  }
}
```

**Usage in Program.cs:**

```csharp
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>()!;
builder.AddProperTelemetry(otelOptions);
```

---

## Distributed Tracing

### Automatic Instrumentation

**These are automatically traced:**

- ✅ HTTP requests (ASP.NET Core, HttpClient)
- ✅ Database queries (Entity Framework Core)
- ✅ Outgoing HTTP calls to other services

**Example trace:**

```
TraceId: abc123
├─ Span: POST /api/auth/register (Landlord BFF) [120ms]
│  Attributes: http.method=POST, http.status_code=200
│  └─ Span: POST http://identity-service/api/token/login [80ms]
│     Attributes: http.target=/api/token/login, peer.service=identity-service
│     └─ Span: INSERT INTO users [15ms]
│        Attributes: db.system=postgresql, db.statement=INSERT INTO users...
```

### Custom Spans

**For business logic tracing:**

```csharp
public class PropertyService
{
    private static readonly ActivitySource ActivitySource = new("ProperTea.Property");

    public async Task<Property> CreatePropertyAsync(CreatePropertyCommand command)
    {
        using var activity = ActivitySource.StartActivity("CreateProperty");
        activity?.SetTag("companyId", command.CompanyId);
        activity?.SetTag("propertyName", command.Name);

        try
        {
            var property = new Property(command.CompanyId, command.Name, command.Address);
            await _repository.AddAsync(property);
            await _unitOfWork.CommitAsync();

            activity?.SetTag("propertyId", property.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return property;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

**Register ActivitySource:**

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("ProperTea.Property") // Add custom source
        // ...
    );
```

### Saga Tracing

**Trace entire saga flow (using GDPR Deletion Saga example):**

```csharp
public class GDPRDeletionSaga : SagaBase
{
    private static readonly ActivitySource ActivitySource = new("ProperTea.Sagas");

    public void RecordValidationResult(string serviceName, bool canDelete, string? reason = null)
    {
        using var activity = ActivitySource.StartActivity("SagaStep.Validation");
        activity?.SetTag("sagaId", Id);
        activity?.SetTag("userId", UserId);
        activity?.SetTag("serviceName", serviceName);
        activity?.SetTag("canDelete", canDelete);
        activity?.SetTag("reason", reason ?? "");
        activity?.SetTag("sagaStatus", Status.ToString());

        ValidationResults[serviceName] = canDelete;
        
        if (!canDelete)
        {
            RecordValidationFailed(new Dictionary<string, string> { [serviceName] = reason });
        }
    }

    public void RecordStepCompleted(string stepName, object? result = null)
    {
        using var activity = ActivitySource.StartActivity($"SagaStep.{stepName}");
        activity?.SetTag("sagaId", Id);
        activity?.SetTag("stepName", stepName);
        activity?.SetTag("sagaStatus", Status.ToString());

        base.RecordStepCompleted(stepName, result);
    }
}
```

**View in Jaeger:**

```
Saga: GDPRDeletion (sagaId: xxx)
├─ Validation: LeaseService (canDelete: false, reason: "2 active leases")
├─ Validation: InvoiceService (canDelete: true)
└─ Validation: MaintenanceService (canDelete: true)
Status: ValidationFailed
Duration: 250ms
```

---

## Logging

### Structured Logging with Serilog

**Configuration:**

```csharp
// Program.cs
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithProperty("ServiceName", "ProperTea.Identity.Service")
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = context.Configuration["OpenTelemetry:Endpoint"]!;
    })
);
```

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Log Correlation

**Logs automatically include trace context:**

```csharp
public async Task<IResult> RegisterUser(RegisterRequest request, ILogger<Register> logger)
{
    logger.LogInformation("User registration started for {Email}", request.Email);
    
    // Business logic...
    
    logger.LogInformation("User {UserId} registered successfully", user.Id);
    
    return Results.Ok();
}
```

**Output:**

```json
{
  "timestamp": "2025-10-22T10:15:30Z",
  "level": "Information",
  "message": "User guid registered successfully",
  "userId": "guid",
  "traceId": "abc123",
  "spanId": "def456",
  "ServiceName": "ProperTea.Identity.Service"
}
```

### Log Queries in Loki

**Find all errors for a user:**

```
{service="ProperTea.Identity.Service"} |= "error" | json | userId="guid"
```

**Find slow queries:**

```
{service="ProperTea.Property.Service"} | json | duration > 1000
```

---

## Metrics

### Custom Metrics

**Define meter:**

```csharp
public class PropertyService
{
    private static readonly Meter Meter = new("ProperTea.Property");
    private static readonly Counter<long> PropertiesCreated = 
        Meter.CreateCounter<long>("properties.created", "count", "Number of properties created");
    private static readonly Histogram<double> PropertyCreationDuration = 
        Meter.CreateHistogram<double>("properties.creation.duration", "ms", "Property creation duration");

    public async Task<Property> CreatePropertyAsync(CreatePropertyCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var property = new Property(command.CompanyId, command.Name, command.Address);
            await _repository.AddAsync(property);
            await _unitOfWork.CommitAsync();

            PropertiesCreated.Add(1, new KeyValuePair<string, object?>("companyId", command.CompanyId));
            PropertyCreationDuration.Record(stopwatch.ElapsedMilliseconds);

            return property;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
```

**Register meter:**

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("ProperTea.Property")
    );
```

### Common Metrics

**HTTP Metrics (automatic):**

- `http.server.request.duration` - Request latency histogram
- `http.server.active_requests` - Concurrent requests gauge
- `http.server.request.body.size` - Request size histogram

**Database Metrics (automatic):**

- `db.client.connections.usage` - Connection pool usage
- `db.client.operation.duration` - Query duration

**Custom Business Metrics:**

- `properties.created` - Properties created counter
- `leases.activated` - Leases activated counter
- `listings.viewed` - Listing views counter
- `saga.duration` - Saga completion time histogram

---

## Dashboards & Alerting

### Grafana Dashboards

**Service Overview Dashboard:**

```yaml
# observability/grafana/dashboards/service-overview.json
{
  "dashboard": {
    "title": "Service Overview",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "rate(http_server_request_duration_count[5m])",
            "legendFormat": "{{service}}"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "rate(http_server_request_duration_count{http_status_code=~\"5..\"}[5m])",
            "legendFormat": "{{service}}"
          }
        ]
      },
      {
        "title": "P95 Latency",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m]))",
            "legendFormat": "{{service}}"
          }
        ]
      }
    ]
  }
}
```

**Saga Dashboard:**

```yaml
{
  "title": "Saga Monitoring",
  "panels": [
    {
      "title": "Active Sagas",
      "targets": [
        {
          "expr": "saga_active_count",
          "legendFormat": "{{saga_type}}"
        }
      ]
    },
    {
      "title": "Saga Success Rate",
      "targets": [
        {
          "expr": "rate(saga_completed_count{status=\"Completed\"}[5m]) / rate(saga_completed_count[5m])",
          "legendFormat": "{{saga_type}}"
        }
      ]
    },
    {
      "title": "Saga Duration",
      "targets": [
        {
          "expr": "histogram_quantile(0.95, rate(saga_duration_bucket[5m]))",
          "legendFormat": "{{saga_type}} p95"
        }
      ]
    }
  ]
}
```

### Prometheus Alerts

```yaml
# observability/prometheus-alerts.yml
groups:
  - name: service_alerts
    rules:
      - alert: HighErrorRate
        expr: rate(http_server_request_duration_count{http_status_code=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Service {{ $labels.service }} has error rate > 5% for 5 minutes"

      - alert: HighLatency
        expr: histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m])) > 1000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency detected"
          description: "Service {{ $labels.service }} p95 latency > 1000ms"

      - alert: SagaFailureSpike
        expr: rate(saga_completed_count{status="Failed"}[5m]) > 0.1
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Saga failure spike"
          description: "Saga {{ $labels.saga_type }} failure rate > 10%"
```

---

## Local vs Production Setup

### Local Development (docker-compose)

**Infrastructure:**

```yaml
# docker-compose.infrastructure.yml
  jaeger:
    image: jaegertracing/all-in-one:latest
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP

  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./observability/prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  loki:
    image: grafana/loki:latest
    ports:
      - "3100:3100"
    command: -config.file=/etc/loki/local-config.yaml

  grafana:
    image: grafana/grafana:latest
    environment:
      - GF_AUTH_ANONYMOUS_ENABLED=true
      - GF_AUTH_ANONYMOUS_ORG_ROLE=Admin
    volumes:
      - ./observability/grafana/datasources:/etc/grafana/provisioning/datasources
      - ./observability/grafana/dashboards:/etc/grafana/provisioning/dashboards
    ports:
      - "3000:3000"
```

**Access:**

- Jaeger UI: http://localhost:16686
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000

### Production (Azure Monitor)

**Configuration:**

```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        });
}
```

**Azure Monitor Features:**

- Application Map (service dependencies visualization)
- Live Metrics (real-time telemetry)
- Log Analytics (Kusto queries)
- Alerts (integrated with Azure Monitor Alerts)
- Workbooks (custom dashboards)

---

**Document Version:**

| Version | Date       | Changes                        |
|---------|------------|--------------------------------|
| 1.0.0   | 2025-10-22 | Initial observability strategy |
