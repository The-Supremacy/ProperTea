using ProperTea.Landlord.Bff.Auth;
using ProperTea.Landlord.Bff.Config;
using ProperTea.Landlord.Bff.Organizations;
using ProperTea.Landlord.Bff.Users;
using ProperTea.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffInfrastructure();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApiConfiguration();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenForwardingHandler>();

builder.Services.AddHttpClient("organization", client =>
{
    var orgServiceUrl = builder.Configuration["services:organization:http:0"]
        ?? builder.Configuration["services:organization:https:0"]
        ?? throw new InvalidOperationException("Organization service URL not configured");
    client.BaseAddress = new Uri(orgServiceUrl);
}).AddHttpMessageHandler<TokenForwardingHandler>();

builder.Services.AddHttpClient("user", client =>
{
    var userServiceUrl = builder.Configuration["services:user:http:0"]
        ?? builder.Configuration["services:user:https:0"]
        ?? throw new InvalidOperationException("User service URL not configured");
    client.BaseAddress = new Uri(userServiceUrl);
}).AddHttpMessageHandler<TokenForwardingHandler>();

builder.Services.AddScoped<OrganizationClient>();
builder.Services.AddScoped<UserClient>();

var app = builder.Build();

app.UseOpenApi(app.Configuration, app.Environment);

if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapUserEndpoints();
app.MapDefaultEndpoints();

app.Run();
