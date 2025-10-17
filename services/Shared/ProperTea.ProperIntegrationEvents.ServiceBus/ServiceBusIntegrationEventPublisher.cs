using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using ProperTea.ProperDdd.Events;
using ProperTea.ProperIntegrationEvents;

namespace ProperTea.ProperIntegrationEvents.ServiceBus;

public class ServiceBusExternalIntegrationEventPublisher : IIntegrationEventPublisher, IExternalIntegrationEventPublisher
{
    private readonly ILogger<ServiceBusExternalIntegrationEventPublisher> _logger;
    private readonly ServiceBusSender _sender;

    public ServiceBusExternalIntegrationEventPublisher(ServiceBusSender sender,
        ILogger<ServiceBusExternalIntegrationEventPublisher> logger)
    {
        this._sender = sender;
        this._logger = logger;
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(@event);
            var message = new ServiceBusMessage(messageBody)
            {
                Subject = topic,
                MessageId = @event.Id.ToString(),
                CorrelationId = @event.Id.ToString()
            };

            await _sender.SendMessageAsync(message, ct);
            _logger.LogInformation("Published integration event {EventType} with ID {EventId}",
                topic, @event.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish integration event {EventType} with ID {EventId}",
                topic, @event.Id);
            throw;
        }
    }
}