using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Publisher;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus;

internal class ServiceBusHealthCheck(IOptions<ServiceBusPublisherConfiguration> options) : IHealthCheck
{
    private readonly ServiceBusPublisherConfiguration _publisherConfiguration = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            await using var client = new ServiceBusClient(_publisherConfiguration.ConnectionString);

            return HealthCheckResult.Healthy("ServiceBus connection is available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ServiceBus connection failed", ex);
        }
    }
}