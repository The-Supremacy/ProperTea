using ProperTea.Landlord.Bff.Config;
using ProperTea.Landlord.Bff.Endpoints;
using ProperTea.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffInfrastructure();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddHttpClient("organization", client =>
{
    var orgServiceUrl = builder.Configuration["services:organization:http:0"]
        ?? builder.Configuration["services:organization:https:0"]
        ?? throw new InvalidOperationException("Organization service URL not configured");
    client.BaseAddress = new Uri(orgServiceUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapDefaultEndpoints();

app.Run();
