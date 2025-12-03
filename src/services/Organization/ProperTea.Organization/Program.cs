using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProperTea.Core.Auth;
using ProperTea.Infrastructure.Auth;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.OpenTelemetry;
using ProperTea.Organization.Configuration;
using ProperTea.Organization.Domain;
using ProperTea.Organization.Features.Organizations;
using ProperTea.Organization.Persistence;
using ProperTea.Organization.Utility;
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        builder.Configuration.Bind("JwtBearer", options);
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); });

var connectionString = builder.Configuration.GetConnectionString("Database")!;
builder.UseWolverine(opts =>
{
    opts.UseFluentValidation();

    builder.Services.AddDbContextWithWolverineIntegration<OrganizationDbContext>(
        x => x.UseNpgsql(connectionString));
    opts.PersistMessagesWithPostgresql(connectionString);
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();

    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableLocalQueues();
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

builder.Services.AddTransient<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddTransient<OrganizationService>();

var app = builder.Build();

app.UseGlobalErrorHandling();

app.UseAuthentication();
app.UseAuthorization();

app.MapOrganizationEndpoints();

app.MapTelemetryEndpoints();
app.MapOpenApi();
app.MapScalarApiReference(o => o
    .AddPreferredSecuritySchemes("BearerAuth")
    .AddHttpAuthentication("BearerAuth", auth =>
    {
        auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
    })
);

return await app.RunJasperFxCommands(args).ConfigureAwait(false);
