using JasperFx;
using ProperTea.ServiceDefaults;
using ProperTea.User.Config;
using ProperTea.User.Features.UserProfiles;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();

builder.Host.ApplyJasperFxExtensions();
builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

var app = builder.Build();

app.UseOpenApi(builder.Configuration, builder.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapDeadLettersEndpoints().RequireAuthorization();

app.MapUserProfileEndpoints();

return await app.RunJasperFxCommands(args);
