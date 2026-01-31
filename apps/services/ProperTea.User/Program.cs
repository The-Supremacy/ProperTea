using JasperFx;
using ProperTea.Infrastructure.Common.ErrorHandling;
using ProperTea.ServiceDefaults;
using ProperTea.User.Config;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

_ = builder.Services.AddHttpContextAccessor();
_ = builder.AddServiceDefaults();
builder.AddGlobalErrorHandling();

builder.Host.ApplyJasperFxExtensions();
builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

builder.Services.AddAuthenticationConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

app.UseGlobalErrorHandling();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapDeadLettersEndpoints().RequireAuthorization();

app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
