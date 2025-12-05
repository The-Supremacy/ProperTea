using JasperFx;
using JasperFx.Resources;
using ProperTea.Core.Auth;
using ProperTea.Infrastructure.Auth;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.OpenApi;
using ProperTea.Infrastructure.OpenTelemetry;
using ProperTea.Organization.Configuration;
using ProperTea.Organization.Domain;
using ProperTea.Organization.Features.Organizations;
using ProperTea.Organization.Persistence;
using Scalar.AspNetCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Core Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Observability & Health
builder.AddProperGlobalErrorHandling(options => { options.ServiceName = "Organization.Api"; });
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>()
                  ?? new OpenTelemetryOptions();
builder.AddProperOpenTelemetry(otelOptions);
builder.AddProperHealthChecks();

// Domain Services
builder.Services.AddTransient<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddTransient<OrganizationDomainService>();

// Infrastructure Extensions (Auth & OpenAPI)
builder.Services.AddProperAuth(builder.Configuration);
builder.Services.AddProperOpenApi();

// Wolverine Configuration
builder.Host.UseWolverine(opts => opts.ConfigureWolverine(builder));
builder.Host.UseResourceSetupOnStartup();

var app = builder.Build();

// Middleware Pipeline
app.UseProperGlobalErrorHandling();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints.
app.MapOrganizationEndpoints();

app.MapTelemetryEndpoints();
app.MapOpenApi();
app.MapScalarApiReference(o => o
    .AddPreferredSecuritySchemes("BearerAuth")
    .AddHttpAuthentication("BearerAuth", auth => { auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; })
);

return await app.RunJasperFxCommands(args).ConfigureAwait(false);
