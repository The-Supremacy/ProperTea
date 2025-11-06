using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TheSupremacy.ProperErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddProperGlobalErrorHandling(this IHostApplicationBuilder builder,
        string? serviceName = null)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                    ?? Guid.NewGuid().ToString();

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(serviceName))
                    context.ProblemDetails.Extensions["service"] = serviceName;

                if (!string.IsNullOrEmpty(context.ProblemDetails.Status?.ToString()))
                    context.ProblemDetails.Type = $"https://httpstatuses.io/{context.ProblemDetails.Status}";
            };
        });

        builder.Services.AddExceptionHandler<ProperGlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseProperGlobalErrorHandling(this WebApplication app, string? serviceName = null)
    {
        app.UseExceptionHandler();

        app.UseStatusCodePages(async context =>
        {
            var problemDetails = ProblemDetailsHelpers.CreateStatusCodeProblemDetails(
                context.HttpContext,
                context.HttpContext.Response.StatusCode,
                serviceName);

            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails);
        });

        return app;
    }
}