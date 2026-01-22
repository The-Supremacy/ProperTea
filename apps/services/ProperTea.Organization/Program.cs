using JasperFx;
using ProperTea.Organization.Config;
using ProperTea.Organization.Features.Organizations;
using ProperTea.Organization.Features.Organizations.Configuration;
using ProperTea.ServiceDefaults;
using ProperTea.ServiceDefaults.ErrorHandling;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

_ = builder.Services.AddHttpContextAccessor();
_ = builder.AddServiceDefaults();

builder.Host.ApplyJasperFxExtensions();
builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

builder.Services.AddAuthenticationConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

builder.Services.AddOrganizationFeature(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

app.UseGlobalErrorHandling();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapDeadLettersEndpoints().RequireAuthorization();

app.MapOrganizationEndpoints();

return await app.RunJasperFxCommands(args);
