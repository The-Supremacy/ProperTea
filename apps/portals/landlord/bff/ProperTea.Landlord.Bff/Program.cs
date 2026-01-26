using Duende.AccessTokenManagement.OpenIdConnect;
using ProperTea.Landlord.Bff.Auth;
using ProperTea.Landlord.Bff.Config;
using ProperTea.Landlord.Bff.Organizations;
using ProperTea.Landlord.Bff.Users;
using ProperTea.ServiceDefaults;
using ProperTea.ServiceDefaults.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffInfrastructure();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApiConfiguration();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenForwardingHandler>();

builder.Services.AddHttpClient<OrganizationClient>(client =>
{
    var orgServiceUrl = builder.Configuration["services:organization:http:0"]
        ?? builder.Configuration["services:organization:https:0"]
        ?? throw new InvalidOperationException("Organization service URL not configured");
    client.BaseAddress = new Uri(orgServiceUrl);
})
.AddUserAccessTokenHandler();

builder.Services.AddHttpClient<UserClient>(client =>
{
    var userServiceUrl = builder.Configuration["services:user:http:0"]
        ?? builder.Configuration["services:user:https:0"]
        ?? throw new InvalidOperationException("User service URL not configured");
    client.BaseAddress = new Uri(userServiceUrl);
})
.AddUserAccessTokenHandler();

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

app.UseGlobalErrorHandling();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapUserEndpoints();
app.MapDefaultEndpoints();

app.Run();
