using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheSupremacy.ProperIntegrationEvents.Transport.ServiceBus.Consumer;

public class ServiceBusIntegrationEventConsumer : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConsumerConfiguration _configuration;
    private readonly IServiceBusMessageProcessor _messageProcessor;
    private readonly ILogger<ServiceBusIntegrationEventConsumer> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusIntegrationEventConsumer(
        IOptions<ServiceBusConsumerConfiguration> options,
        IServiceBusMessageProcessor messageProcessor,
        ILogger<ServiceBusIntegrationEventConsumer> logger)
    {
        _configuration = options.Value;
        _messageProcessor = messageProcessor;
        _logger = logger;

        ValidateConfiguration();

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = _configuration.MaxRetries,
                Delay = _configuration.RetryDelay,
                Mode = ServiceBusRetryMode.Exponential
            }
        };

        _client = new ServiceBusClient(_configuration.ConnectionString, clientOptions);
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_processor != null)
        {
            _logger.LogWarning("Consumer already started");
            return;
        }

        var processorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false, // We handle completion manually
            MaxConcurrentCalls = _configuration.MaxConcurrentMessages,
            PrefetchCount = _configuration.PrefetchCount
        };

        _processor = _client.CreateProcessor(
            _configuration.TopicName,
            _configuration.SubscriptionName,
            processorOptions);

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        _logger.LogInformation(
            "Starting ServiceBus consumer for topic '{Topic}' subscription '{Subscription}'",
            _configuration.TopicName, _configuration.SubscriptionName);

        await _processor.StartProcessingAsync(ct);
    }
    

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_processor == null)
        {
            _logger.LogWarning("Consumer not started");
            return;
        }

        _logger.LogInformation("Stopping ServiceBus consumer");
        await _processor.StopProcessingAsync(ct);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var messageActions = new ServiceBusMessageActions(args);
        await _messageProcessor.ProcessMessageAsync(args.Message, messageActions, args.CancellationToken);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "ServiceBus processor error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_configuration.ConnectionString))
            throw new InvalidOperationException("ServiceBus ConnectionString is required");

        if (string.IsNullOrWhiteSpace(_configuration.TopicName))
            throw new InvalidOperationException("ServiceBus TopicName is required");

        if (string.IsNullOrWhiteSpace(_configuration.SubscriptionName))
            throw new InvalidOperationException("ServiceBus SubscriptionName is required");
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }

        await _client.DisposeAsync();
    }
}
