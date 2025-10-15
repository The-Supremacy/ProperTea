using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Identity.Service.Configuration;
using ProperTea.Identity.Service.Data;
using ProperTea.Identity.Service.Endpoints;
using ProperTea.Identity.Service.Models;
using ProperTea.Identity.Service.Services;
using ProperTea.ProperErrorHandling;
using ProperTea.ProperTelemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.AddProperGlobalErrorHandling("ProperTea.Identity.Service");

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
;
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
namespace ProperTea.Identity.Service
{
    public class Program
    {
    }
}