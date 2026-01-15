using JasperFx;
using ProperTea.Organization.Config;
using ProperTea.Organization.Features.Organizations;
using ProperTea.Organization.Features.Organizations.Configuration;
using ProperTea.ServiceDefaults;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();

builder.Host.ApplyJasperFxExtensions();
builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

// Feature registrations
builder.Services.AddOrganizationFeature(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseOpenApi(builder.Configuration, builder.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapDeadLettersEndpoints().RequireAuthorization();

app.MapOrganizationEndpoints();

return await app.RunJasperFxCommands(args);
