using Microsoft.EntityFrameworkCore;
using ProperTea.Identity.Kernel.Data;
using ProperTea.Identity.Kernel.IntegrationEvents;
using ProperTea.Identity.Worker.Workers;
using ProperTea.ProperIntegrationEvents;
using ProperTea.ProperIntegrationEvents.Kafka;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;
using ProperTea.ProperIntegrationEvents.ServiceBus;
using TheSupremacy.ProperErrorHandling;
using TheSupremacy.ProperTelemetry;

var builder = Host.CreateApplicationBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() ??
                  new OpenTelemetryOptions();
builder.AddProperTelemetry(otelOptions);

builder.AddProperGlobalErrorHandling("ProperTea.Identity.Worker");

builder.Services.AddDbContext<ProperTeaIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var integrationEventsBuilder = builder.Services.AddProperIntegrationEvents(e =>
{
    e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName);
});
integrationEventsBuilder.AddOutbox().AddEntityFrameworkStores<ProperTeaIdentityDbContext>();
if (isDevelopment)
    integrationEventsBuilder.AddKafka(kafka =>
    {
        kafka.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        kafka.ClientId = "identity-worker";
        kafka.CompressionType = CompressionType.Snappy;
    });
else
    integrationEventsBuilder.AddServiceBus(sb =>
    {
        sb.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
        sb.MaxRetries = 5;
        sb.RetryDelay = TimeSpan.FromSeconds(3);
        sb.ClientId = "identity-worker";
    });

builder.Services.Configure<OutboxProcessorOptions>(
    builder.Configuration.GetSection("OutboxProcessor"));

builder.Services.AddHostedService<OutboxProcessorWorker>();

var host = builder.Build();
host.Run();