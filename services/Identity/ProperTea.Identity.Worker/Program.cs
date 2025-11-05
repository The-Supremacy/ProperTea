using Microsoft.EntityFrameworkCore;
using ProperTea.Identity.Service.Data;
using ProperTea.Identity.Service.IntegrationEvents;
using ProperTea.Identity.Worker.Publishers;
using ProperTea.Identity.Worker.Workers;
using ProperTea.ProperErrorHandling;
using ProperTea.ProperIntegrationEvents;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;
using ProperTea.ProperTelemetry;

var builder = Host.CreateApplicationBuilder(args);
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() ?? 
                  new OpenTelemetryOptions();
builder.AddProperTelemetry(otelOptions);

builder.AddProperGlobalErrorHandling("ProperTea.Identity.Worker");

builder.Services.AddDbContext<ProperTeaIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddProperIntegrationEvents(e =>
{
    e.AddEventType<UserCreatedIntegrationEvent>("UserCreated");
}).UseOutbox().UseEntityFrameworkStorage<ProperTeaIdentityDbContext>();

// TODO: Add actual message bus publisher (RabbitMQ/Azure Service Bus)
builder.Services.AddScoped<IExternalIntegrationEventPublisher, NoOpExternalIntegrationEventPublisher>();

builder.Services.Configure<OutboxProcessorOptions>(
    builder.Configuration.GetSection("OutboxProcessor"));

builder.Services.AddHostedService<OutboxProcessorWorker>();

var host = builder.Build();
host.Run();
