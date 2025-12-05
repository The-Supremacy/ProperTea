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

var openApiOptions = new ProperOpenApiOptions();
builder.Configuration.GetSection(ProperOpenApiOptions.SectionName).Bind(openApiOptions);
builder.Services.AddSingleton(openApiOptions);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<HttpsServerSchemeTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    if (!string.IsNullOrEmpty(openApiOptions.AuthorizationUrl))
    {
        options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
    }
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

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ProperTea API")
            .AddPreferredSecuritySchemes("OAuth2")
            .AddAuthorizationCodeFlow("oauth2", flow =>
            {
                flow.ClientId = openApiOptions.ClientId;
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = openApiOptions.Scopes;
            });
    });
}

return await app.RunJasperFxCommands(args).ConfigureAwait(false);
