using JasperFx;
using JasperFx.Resources;
using Microsoft.OpenApi.Models;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<HttpsServerSchemeTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
});

// Wolverine Configuration
builder.Host.UseWolverine(opts => opts.ConfigureWolverine(builder));
builder.Host.UseResourceSetupOnStartup();

var app = builder.Build();

// Middleware Pipeline
app.UseProperGlobalErrorHandling();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints.
app.MapOrganizationEndpoints();

app.MapTelemetryEndpoints();

if (builder.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ProperTea API")
            .AddPreferredSecuritySchemes("OAuth2")
            .AddAuthorizationCodeFlow("oauth2", flow =>
            {
                flow.ClientId = "propertea-organization-api";
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = ["openid", "profile", "email"];
            })
            .AddHttpAuthentication("BearerAuth", auth =>
            {
                auth.Token = "vJs21pDJcTzuNJHJBbbp8ALPHufX4wFuoC1Kuu0mqTcCxrpfRFShoC6rujU7";
            });;
    });
}

return await app.RunJasperFxCommands(args).ConfigureAwait(false);
