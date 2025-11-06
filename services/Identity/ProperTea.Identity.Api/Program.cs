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
using TheSupremacy.ProperCqrs;
using ProperTea.ProperDdd;
using ProperTea.ProperDdd.Persistence.Ef;
using TheSupremacy.ProperErrorHandling;
using ProperTea.ProperIntegrationEvents;
using ProperTea.ProperIntegrationEvents.Kafka;
using ProperTea.ProperIntegrationEvents.Outbox;
using ProperTea.ProperIntegrationEvents.Outbox.Ef;
using ProperTea.ProperIntegrationEvents.ServiceBus;
using TheSupremacy.ProperTelemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.AddProperGlobalErrorHandling("ProperTea.Identity.Api");

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
builder.Services.AddProperDdd()
    .UseEntityFramework<ProperTeaIdentityDbContext>();
builder.Services.AddProperCqrs(typeof(Program).Assembly);

builder.Services.Configure<OutboxConfiguration>(builder.Configuration.GetSection("Outbox"));
var integrationEventsBuilder = builder.Services.AddProperIntegrationEvents(e =>
{
    e.AddEventType<UserCreatedIntegrationEvent>(UserCreatedIntegrationEvent.EventTypeName);
});
integrationEventsBuilder.AddOutbox().AddEntityFrameworkStores<ProperTeaIdentityDbContext>();

if (isDevelopment)
    integrationEventsBuilder.AddKafka(kafka =>
    {
        kafka.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        kafka.ClientId = "identity-api";
        kafka.CompressionType = CompressionType.Snappy;
    });
else
    integrationEventsBuilder.AddServiceBus(sb =>
    {
        sb.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
        sb.MaxRetries = 5;
        sb.RetryDelay = TimeSpan.FromSeconds(3);
        sb.ClientId = "identity-api";
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseProperGlobalErrorHandling();
app.MapProperTelemetryEndpoints();

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