using Microsoft.AspNetCore.DataProtection;
using ProperTea.Infrastructure.ErrorHandling;
using ProperTea.Infrastructure.OpenTelemetry;
using StackExchange.Redis;

namespace ProperTea.Landlord.Bff.Config
{
    public static class InfrastructureConfig
    {
        public static IHostApplicationBuilder AddBffInfrastructure(this IHostApplicationBuilder builder)
        {
            // 1. Shared Error Handling
            _ = builder.AddProperGlobalErrorHandling(options =>
            {
                options.ServiceName = "Landlord.Bff";
            });

            // 2. Shared OpenTelemetry
            var otelOptions = new OpenTelemetryOptions();
            builder.Configuration.GetSection("OpenTelemetry").Bind(otelOptions);
            _ = builder.AddProperOpenTelemetry(otelOptions);

            // 3. Redis & Data Protection
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                                        ?? throw new InvalidOperationException("Redis connection string is missing.");

            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
            _ = builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

            _ = builder.Services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys")
                .SetApplicationName("ProperTea.Landlord");

            _ = builder.Services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnectionString);

            return builder;
        }
    }
}
