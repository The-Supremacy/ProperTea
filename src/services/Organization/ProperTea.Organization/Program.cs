using Marten;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.OpenTelemetry;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddGlobalErrorHandling(options => { options.ServiceName = "Organization.Api"; });

var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>()
                  ?? new OpenTelemetryOptions();
builder.AddOpenTelemetry(otelOptions);
builder.AddProperHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add Marten for data storage
builder.Services.AddMarten(opts => { opts.Connection(builder.Configuration.GetConnectionString("Database")!); })
    .IntegrateWithWolverine();

// Add and configure Wolverine
builder.Host.UseWolverine();

var app = builder.Build();

app.UseGlobalErrorHandling();

app.MapProperTelemetryEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
