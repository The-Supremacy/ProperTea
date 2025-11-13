using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Identity.Api.Configuration;
using ProperTea.Identity.Api.Endpoints;
using ProperTea.Identity.Kernel.Data;
using ProperTea.Identity.Kernel.IntegrationEvents;
using ProperTea.Identity.Kernel.Models;
using ProperTea.Identity.Kernel.Services;
using TheSupremacy.ProperDomain.Persistence.Ef;
using TheSupremacy.ProperIntegrationEvents;
using TheSupremacy.ProperIntegrationEvents.Outbox;
using TheSupremacy.ProperIntegrationEvents.Persistence.Ef;
using TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus;
using Scalar.AspNetCore;
using TheSupremacy.ProperCqrs;
using TheSupremacy.ProperDomain;
using TheSupremacy.ProperErrorHandling;
using TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Publisher;
using TheSupremacy.ProperTelemetry;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.AddProperGlobalErrorHandling(o =>
{
    o.ServiceName = "ProperTea.Identity.Api";
});

// OTel.
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() ??
                  new OpenTelemetryOptions();
builder.AddProperTelemetry(otelOptions);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database-check");

// DB.
builder.Services.AddDbContext<ProperTeaIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity.
var identitySettings = builder.Configuration.GetSection("IdentitySettings")
    .Get<IdentitySettings>() ?? new IdentitySettings();

builder.Services.AddIdentity<ProperTeaUser, IdentityRole<Guid>>(options =>
    {
        options.Password = new PasswordOptions
        {
            RequireDigit = identitySettings.Password.RequireDigit,
            RequiredLength = identitySettings.Password.RequiredLength,
            RequireNonAlphanumeric = identitySettings.Password.RequireNonAlphanumeric,
            RequireUppercase = identitySettings.Password.RequireUppercase,
            RequireLowercase = identitySettings.Password.RequireLowercase
        };

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    })
    .AddEntityFrameworkStores<ProperTeaIdentityDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication.
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });
var googleAuthSettings = builder.Configuration.GetSection("GoogleAuthSettings");
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = googleAuthSettings["ClientId"]!;
        options.ClientSecret = googleAuthSettings["ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();

// Proper registrations.
builder.Services.AddProperDomain()
    .UseEntityFramework<ProperTeaIdentityDbContext>();
builder.Services.AddProperCqrs(typeof(Program).Assembly);

builder.Services.Configure<OutboxConfiguration>(builder.Configuration.GetSection("Outbox"));
var integrationEventsBuilder = builder.Services.AddProperIntegrationEvents(e =>
{
    e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName);
});
integrationEventsBuilder
    .AddOutbox()
    .AddEntityFrameworkStores<ProperTeaIdentityDbContext>()
    .AddServiceBusTransport(sb =>
    {
        sb.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
        sb.MaxRetries = 5;
        sb.RetryDelay = TimeSpan.FromSeconds(3);
        sb.ClientId = "identity-worker";
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProperTelemetryEndpoints();
app.UseProperGlobalErrorHandling();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapApplicationEndpoints();

app.Run();

// This is needed so that test can access the app.
namespace ProperTea.Identity.Api
{
    public class Program
    {
    }
}