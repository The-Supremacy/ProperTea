using ProperTea.Landlord.Bff.Config;
using ProperTea.Landlord.Bff.Endpoints;
using ProperTea.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffInfrastructure();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddBffProxy(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
