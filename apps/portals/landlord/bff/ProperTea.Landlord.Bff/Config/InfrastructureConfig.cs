using Microsoft.AspNetCore.DataProtection;
using ProperTea.ServiceDefaults;
using StackExchange.Redis;

namespace ProperTea.Landlord.Bff.Config
{
    public static class InfrastructureConfig
    {
        public static IHostApplicationBuilder AddBffInfrastructure(this IHostApplicationBuilder builder)
        {
            _ = builder.AddServiceDefaults();

            var redisConnectionString = builder.Configuration.GetConnectionString("redis")
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
