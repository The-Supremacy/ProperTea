using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ProperTea.Infrastructure.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddProperGlobalErrorHandling(this IHostApplicationBuilder builder,
        Action<ErrorHandlingOptions>? configure = null)
    {
        _ = builder.Services.Configure(configure ?? (_ => { }));

        _ = builder.Services.AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = context =>
            {
                var options = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<ErrorHandlingOptions>>().Value;

                var correlationId = CorrelationIdProvider.GetOrCreate(context.HttpContext);

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(options.ServiceName))
                {
                    context.ProblemDetails.Extensions["service"] = options.ServiceName;
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

    public static WebApplication UseProperGlobalErrorHandling(this WebApplication app)
    {
        _ = app.UseExceptionHandler();
        _ = app.UseStatusCodePages();

        return app;
    }
}
