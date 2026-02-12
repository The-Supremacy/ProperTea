using Duende.AccessTokenManagement.OpenIdConnect;
using ProperTea.Infrastructure.Common.ErrorHandling;
using ProperTea.Landlord.Bff.Auth;
using ProperTea.Landlord.Bff.Companies;
using ProperTea.Landlord.Bff.Config;
using ProperTea.Landlord.Bff.Organizations;
using ProperTea.Landlord.Bff.Property;
using ProperTea.Landlord.Bff.Session;
using ProperTea.Landlord.Bff.Users;
using ProperTea.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffInfrastructure();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApiConfiguration();
builder.AddGlobalErrorHandling();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<OrganizationHeaderHandler>();

builder.Services.AddHttpClient<OrganizationClient>(client =>
{
    var orgServiceUrl = builder.Configuration["services:organization:http:0"]
        ?? builder.Configuration["services:organization:https:0"]
        ?? throw new InvalidOperationException("Organization service URL not configured");
    client.BaseAddress = new Uri(orgServiceUrl);
})
.AddUserAccessTokenHandler()
.AddHttpMessageHandler<OrganizationHeaderHandler>();

// Separate anon service because Duende doesn't support anonymous clients.
builder.Services.AddHttpClient<OrganizationClientAnonymous>(client =>
{
    var orgServiceUrl = builder.Configuration["services:organization:http:0"]
        ?? builder.Configuration["services:organization:https:0"]
        ?? throw new InvalidOperationException("Organization service URL not configured");
    client.BaseAddress = new Uri(orgServiceUrl);
});

builder.Services.AddHttpClient<UserClient>(client =>
{
    var userServiceUrl = builder.Configuration["services:user:http:0"]
        ?? builder.Configuration["services:user:https:0"]
        ?? throw new InvalidOperationException("User service URL not configured");
    client.BaseAddress = new Uri(userServiceUrl);
})
.AddUserAccessTokenHandler()
.AddHttpMessageHandler<OrganizationHeaderHandler>();

builder.Services.AddHttpClient<CompanyClient>(client =>
{
    var companyServiceUrl = builder.Configuration["services:company:http:0"]
        ?? builder.Configuration["services:company:https:0"]
        ?? throw new InvalidOperationException("Company service URL not configured");
    client.BaseAddress = new Uri(companyServiceUrl);
})
.AddUserAccessTokenHandler()
.AddHttpMessageHandler<OrganizationHeaderHandler>();

builder.Services.AddHttpClient<PropertyClient>(client =>
{
    var propertyServiceUrl = builder.Configuration["services:property:http:0"]
        ?? builder.Configuration["services:property:https:0"]
        ?? throw new InvalidOperationException("Property service URL not configured");
    client.BaseAddress = new Uri(propertyServiceUrl);
})
.AddUserAccessTokenHandler()
.AddHttpMessageHandler<OrganizationHeaderHandler>();

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

app.UseGlobalErrorHandling();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapSessionEndpoints();
app.MapOrganizationEndpoints();
app.MapUserEndpoints();
app.MapCompanyEndpoints();
app.MapPropertyEndpoints();
app.MapDefaultEndpoints();

app.Run();
