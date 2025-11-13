using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TheSupremacy.ProperErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddProperGlobalErrorHandling(this IHostApplicationBuilder builder,
        Action<ErrorHandlingOptions>? configure = null)
    {
        builder.Services.Configure(configure ?? (_ => { }));
        
        builder.Services.AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = context =>
            {
                var options = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<ErrorHandlingOptions>>().Value;
                
                var correlationId = CorrelationIdProvider.GetOrCreate(context.HttpContext);

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(options.ServiceName))
                    context.ProblemDetails.Extensions["service"] = options.ServiceName;
            
                if (context.ProblemDetails.Status.HasValue)
                    context.ProblemDetails.Type = $"{options.ProblemDetailsTypeBaseUrl}/{context.ProblemDetails.Status}";
            };
        });

        builder.Services.AddExceptionHandler<ProperGlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseProperGlobalErrorHandling(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ErrorHandlingOptions>>();
        
        app.UseExceptionHandler();

        app.UseStatusCodePages(async context =>
        {
            var problemDetails = ProblemDetailsHelpers.CreateStatusCodeProblemDetails(
                context.HttpContext,
                context.HttpContext.Response.StatusCode,
                options.Value.ProblemDetailsTypeBaseUrl,
                options.Value.ServiceName);

            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails);
        });

        return app;
    }
}