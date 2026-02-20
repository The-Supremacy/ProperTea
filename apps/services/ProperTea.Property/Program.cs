using JasperFx;
using System.Text.Json.Serialization;
using ProperTea.Property.Features.Buildings.Configuration;
using ProperTea.Property.Features.Companies.Configuration;
using ProperTea.Property.Features.Properties.Configuration;
using ProperTea.Property.Features.Units.Configuration;
using ProperTea.Infrastructure.Common.ErrorHandling;
using ProperTea.ServiceDefaults;
using Wolverine.Http;
using ProperTea.Property.Configuration;

var builder = WebApplication.CreateBuilder(args);

_ = builder.Services.AddHttpContextAccessor();
_ = builder.AddServiceDefaults();
_ = builder.AddGlobalErrorHandling();

builder.Host.ApplyJasperFxExtensions();
builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

// Enums are sent as strings from the Angular frontend; accept both string names and integers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthenticationConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

builder.Services.AddPropertyFeature();
builder.Services.AddBuildingFeature();
builder.Services.AddUnitFeature();
builder.Services.AddCompanyReferenceHttpClient();

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

app.UseGlobalErrorHandling();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapDeadLettersEndpoints().RequireAuthorization();

app.MapWolverineEndpoints(opts =>
{
    opts.WarmUpRoutes = RouteWarmup.Eager;
});

return await app.RunJasperFxCommands(args);
