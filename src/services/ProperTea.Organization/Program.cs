using JasperFx;
using ProperTea.Organization.Config;
using ProperTea.Organization.Features.Organization.Create;
using ProperTea.Organization.Infrastructure.Zitadel;
using ProperTea.ServiceDefaults;
using Zitadel.Credentials;

var builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();

builder.Services.AddMartenConfiguration(builder.Configuration, builder.Environment);
builder.Host.AddWolverineConfiguration();

builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddOpenApiConfiguration(builder.Configuration);

builder.Services.AddSingleton<IZitadelClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ZitadelClient>>();

    var apiUrl = config["OIDC:Authority"]
        ?? throw new InvalidOperationException("OIDC:Authority not configured");

    var serviceAccountPath = config["Zitadel:ServiceAccountPath"]
        ?? throw new InvalidOperationException("Zitadel:ServiceAccountPath not configured");

    var serviceAccount = ServiceAccount.LoadFromJsonFile(serviceAccountPath);

    return new ZitadelClient(apiUrl, serviceAccount, logger);
});

var app = builder.Build();

app.UseOpenApi(builder.Configuration, builder.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

var orgsGroup = app.MapGroup("/organizations")
    .WithTags("Organizations");
orgsGroup.MapCreateOrganizationEndpoints();

return await app.RunJasperFxCommands(args);
