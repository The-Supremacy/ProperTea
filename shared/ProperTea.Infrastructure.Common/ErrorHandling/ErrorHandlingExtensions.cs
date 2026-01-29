using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProperTea.ServiceDefaults.ErrorHandling;

namespace ProperTea.Infrastructure.Common.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddGlobalErrorHandling(this IHostApplicationBuilder builder)
    {
        _ = builder.Services.AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = context =>
            {
                var options = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<ErrorHandlingOptions>>().Value;
                var env = context.HttpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>();

                var correlationId = CorrelationIdProvider.GetOrCreate(context.HttpContext);

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(options.ServiceName))
                {
                    context.ProblemDetails.Extensions["service"] = options.ServiceName;
                }

                if (env.IsDevelopment() && context.Exception != null)
                {
                    context.ProblemDetails.Extensions["exception"] = new
                    {
                        type = context.Exception.GetType().FullName,
                        message = context.Exception.Message,
                        stackTrace = context.Exception.StackTrace,
                        innerException = context.Exception.InnerException?.Message
                    };
                }

                if (context.ProblemDetails.Status.HasValue)
                {
                    context.ProblemDetails.Type =
                        $"{options.ProblemDetailsTypeBaseUrl}/{context.ProblemDetails.Status}";
                }
            };
        });

        _ = builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseGlobalErrorHandling(this WebApplication app)
    {
        _ = app.UseExceptionHandler();
        _ = app.UseStatusCodePages();

        return app;
    }
}
