using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.OpenTelemetry;
using ProperTea.Organization.Configuration;
using ProperTea.Organization.Domain;
using ProperTea.Organization.Features.Organizations.Endpoints;
using ProperTea.Organization.Persistence;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddGlobalErrorHandling(options => { options.ServiceName = "Organization.Api"; });

var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>()
                  ?? new OpenTelemetryOptions();
builder.AddOpenTelemetry(otelOptions);
builder.AddProperHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Database")!;
builder.Services.AddDbContext<OrganizationDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
}, ServiceLifetime.Singleton);
builder.Services.AddDbContextWithWolverineIntegration<OrganizationDbContext>(
    x => x.UseNpgsql(connectionString));
builder.UseWolverine(opts =>
{
    opts.UseFluentValidation();

    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableLocalQueues();

    opts.PersistMessagesWithPostgresql(connectionString);

    opts.Include<MessagingExtension>();
    if (builder.Environment.IsDevelopment())
    {
        var rabbitMqConn = builder.Configuration.GetConnectionString("RabbitMq")!;
        opts.UseRabbitMq(rabbitMqConn).AutoProvision();

        opts.Durability.Mode = DurabilityMode.Solo;
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
    }
    else
    {
        var serviceBusConn = builder.Configuration.GetConnectionString("ServiceBus")!;
        opts.UseAzureServiceBus(serviceBusConn).AutoProvision();

        opts.Durability.Mode = DurabilityMode.Balanced;
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }
});
builder.Host.UseResourceSetupOnStartup();
builder.Services.AddWolverineHttp();

builder.Services.AddTransient<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddTransient<OrganizationService>();

var app = builder.Build();

app.UseGlobalErrorHandling();

app.MapOrganizationEndpoints();

app.MapTelemetryEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();

return await app.RunJasperFxCommands(args).ConfigureAwait(false);
