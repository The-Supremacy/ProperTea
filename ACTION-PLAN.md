# 🎯 ProperTea - Prioritized Action Plan

**Date:** November 5, 2025  
**Status:** Identity Phase 1a Complete - Planning Next Steps

---

## 📊 Current State Analysis

### ✅ What's Complete

1. **Identity Service API** - Outbox pattern implemented
2. **Identity Worker** - Processes outbox messages
3. **All Shared Libraries** - ProperSagas, ProperIntegrationEvents, ProperDdd, etc.
4. **Tests** - All existing tests passing
5. **Documentation** - Complete architecture docs

### 🔧 What Needs Work

#### 1. Identity Service (Remaining TODOs)

- ❌ Using NoOp publisher (not real message bus)
- ❌ No Kafka support for local development
- ❌ ServiceBus publisher has resource leak (sender not disposed)
- ❌ No consumer/subscriber implementation yet
- ❌ No integration tests for event flow

#### 2. Landlord BFF

- ❌ No JWT enrichment (org/permissions)
- ❌ No multi-org support
- ❌ Session structure needs update
- ❌ May or may not need Sagas (need to analyze)

#### 3. Development Modes

- ❌ Mode 1 (Inner Loop) - Partially working
- ❌ Mode 2 (Application Loop) - Not implemented
- ❌ Mode 3 (Integration Testing) - Not implemented
- ❌ Mode 4 (Kind) - Not implemented

---

## 🎯 Recommended Prioritization

### Option A: Complete Identity First (Recommended) ⭐

**Rationale:** Finish what we started, prove the patterns work end-to-end

**Priority Order:**

1. ✅ Fix ServiceBus publisher resource leak
2. ✅ Add Kafka support for local development
3. ✅ Implement event consumer in Identity Worker (as example)
4. ✅ Write integration tests for outbox + Kafka
5. ⏭️ Move to Contact/Organization services

**Timeline:** 2-3 days

**Benefits:**

- Complete end-to-end event flow working
- Pattern proven and documented
- Can replicate in other services
- Real Kafka integration working locally

---

### Option B: Implement Development Modes

**Rationale:** Enable better development workflow before building more services

**Priority Order:**

1. ✅ Mode 1 (Inner Loop) - Fix any remaining issues
2. ✅ Mode 2 (Application Loop) - Docker Compose for all services
3. ✅ Create Makefile commands
4. ⏭️ Mode 3 & 4 later

**Timeline:** 3-4 days

**Benefits:**

- Better DX (developer experience)
- Easier to test multi-service scenarios
- Documented workflow for team

---

### Option C: Build Contact Service Next

**Rationale:** Prove service-to-service communication via events

**Priority Order:**

1. ✅ Build Contact Service (API + Worker)
2. ✅ Consume UserCreated events
3. ✅ Test choreography pattern
4. ⏭️ Back to fix Identity TODOs

**Timeline:** 4-5 days

**Benefits:**

- Validates architecture works across services
- Real choreography example
- Identifies gaps in shared libraries

---

## 💡 My Recommendation: **Hybrid Approach**

### Week 1: Complete Identity + Basic Dev Mode

**Day 1-2: Fix Identity TODOs**

1. Fix ServiceBus publisher resource leak
2. Add Kafka publisher implementation
3. Update Worker to use Kafka for local dev
4. Add basic event consumer example

**Day 3: Development Mode 1**

5. Document Mode 1 (Inner Loop) setup
6. Create quick-start scripts
7. Test with current services

**Day 4-5: Integration Tests**

8. Write integration tests for outbox pattern
9. Test Kafka event flow end-to-end
10. Document testing approach

### Week 2: Development Mode 2 + Landlord BFF

**Day 1-3: Application Loop Mode**

1. Create docker-compose for all services
2. Add Makefile commands
3. Document multi-service debugging

**Day 4-5: Analyze Landlord BFF**

4. Review current implementation
5. Identify what needs refactoring
6. Plan JWT enrichment approach

---

## 📋 Detailed Action Items

### 🔥 Priority 1: Fix Critical Issues

#### 1.1 Fix ServiceBus Publisher Resource Leak

**File:** `ProperTea.ProperIntegrationEvents.ServiceBus/ServiceBusIntegrationEventPublisher.cs`

**Problem:**

```csharp
ServiceBusSender sender = null;
try
{
    sender = _client.CreateSender(topic);
    // ... use sender
}
catch (Exception ex)
{
    throw;
}
// ❌ Sender never disposed!
```

**Solution:**

```csharp
public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
    where TEvent : IntegrationEvent
{
    await using var sender = _client.CreateSender(topic);
    
    var messageBody = JsonSerializer.Serialize(@event);
    var message = new ServiceBusMessage(messageBody)
    {
        Subject = topic,
        MessageId = @event.Id.ToString(),
        CorrelationId = @event.Id.ToString()
    };

    try
    {
        await sender.SendMessageAsync(message, ct);
        _logger.LogInformation("Published integration event {EventType} with ID {EventId}",
            typeof(TEvent).Name, @event.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to publish integration event {EventType} with ID {EventId}",
            typeof(TEvent).Name, @event.Id);
        throw;
    }
}
```

---

#### 1.2 Add Kafka Publisher

**File:** Create `ProperTea.ProperIntegrationEvents.Kafka/KafkaIntegrationEventPublisher.cs`

**Implementation:**

```csharp
using Confluent.Kafka;
using System.Text.Json;

namespace ProperTea.ProperIntegrationEvents.Kafka;

public class KafkaExternalIntegrationEventPublisher : IExternalIntegrationEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaExternalIntegrationEventPublisher> _logger;

    public KafkaExternalIntegrationEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaExternalIntegrationEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(@event);
            var message = new Message<string, string>
            {
                Key = @event.Id.ToString(),
                Value = messageBody,
                Headers = new Headers
                {
                    { "EventType", Encoding.UTF8.GetBytes(typeof(TEvent).Name) },
                    { "CorrelationId", Encoding.UTF8.GetBytes(@event.Id.ToString()) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, ct);
            
            _logger.LogInformation(
                "Published integration event {EventType} with ID {EventId} to topic {Topic}, Partition: {Partition}, Offset: {Offset}",
                typeof(TEvent).Name, @event.Id, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish integration event {EventType} with ID {EventId} to topic {Topic}",
                typeof(TEvent).Name, @event.Id, topic);
            throw;
        }
    }
}
```

**Registration Extension:**

```csharp
// ProperTea.ProperIntegrationEvents.Kafka/KafkaIntegrationEventsBuilderExtensions.cs
public static class KafkaIntegrationEventsBuilderExtensions
{
    public static IntegrationEventsBuilder UseKafka(
        this IntegrationEventsBuilder builder,
        Action<ProducerConfig> configure)
    {
        // Configure Kafka producer
        var producerConfig = new ProducerConfig();
        configure(producerConfig);

        builder.Services.AddSingleton<IProducer<string, string>>(sp =>
        {
            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        builder.Services.TryAddSingleton<IExternalIntegrationEventPublisher, 
            KafkaExternalIntegrationEventPublisher>();

        // Finalize type resolver
        builder.EnsureTypeResolverRegistered();

        return builder;
    }
}
```

---

#### 1.3 Add Kafka Consumer Base Class

**File:** Create `ProperTea.ProperIntegrationEvents.Kafka/KafkaIntegrationEventConsumer.cs`

```csharp
using Confluent.Kafka;
using System.Text.Json;

namespace ProperTea.ProperIntegrationEvents.Kafka;

public abstract class KafkaIntegrationEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IIntegrationEventTypeResolver _typeResolver;
    private readonly ILogger _logger;
    private readonly string[] _topics;

    protected KafkaIntegrationEventConsumer(
        IConsumer<string, string> consumer,
        IIntegrationEventTypeResolver typeResolver,
        ILogger logger,
        params string[] topics)
    {
        _consumer = consumer;
        _typeResolver = typeResolver;
        _logger = logger;
        _topics = topics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topics);
        _logger.LogInformation("Kafka consumer started, subscribed to topics: {Topics}", 
            string.Join(", ", _topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessageAsync(consumeResult, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        try
        {
            var eventTypeHeader = result.Message.Headers
                .FirstOrDefault(h => h.Key == "EventType");
            
            if (eventTypeHeader == null)
            {
                _logger.LogWarning("Message missing EventType header, skipping");
                return;
            }

            var eventTypeName = Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
            var eventType = _typeResolver.ResolveType(eventTypeName);

            if (eventType == null)
            {
                _logger.LogWarning("Unknown event type: {EventType}", eventTypeName);
                return;
            }

            var integrationEvent = JsonSerializer.Deserialize(result.Message.Value, eventType) 
                as IntegrationEvent;

            if (integrationEvent != null)
            {
                await HandleEventAsync(integrationEvent, ct);
                _consumer.Commit(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            // Don't commit - let Kafka retry
        }
    }

    protected abstract Task HandleEventAsync(IntegrationEvent @event, CancellationToken ct);
}
```

---

### 🎯 Priority 2: Update Identity Worker

**File:** `ProperTea.Identity.Worker/Program.cs`

**Add environment-based publisher selection:**

```csharp
// Add Kafka for local development, ServiceBus for production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddProperIntegrationEvents(e =>
    {
        e.AddEventType<UserCreatedIntegrationEvent>("UserCreated");
    })
    .UseKafka(config =>
    {
        config.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] 
            ?? "localhost:9092";
        config.ClientId = "identity-worker";
    })
    .UseOutbox()
    .UseEntityFrameworkStorage<ProperTeaIdentityDbContext>();
}
else
{
    builder.Services.AddProperIntegrationEvents(e =>
    {
        e.AddEventType<UserCreatedIntegrationEvent>("UserCreated");
    })
    .UseServiceBus()
    .UseOutbox()
    .UseEntityFrameworkStorage<ProperTeaIdentityDbContext>();
}
```

---

### 🎯 Priority 3: Landlord BFF Analysis

**Question 1: Does BFF need Sagas?**

**Analysis:**

- BFF is stateless gateway/aggregator
- Session management is simple key-value (Redis)
- No multi-step workflows initiated by BFF
- JWT enrichment is synchronous (call Permission Service, enrich, cache)

**Answer:** ❌ **No Sagas needed in BFF**

**BFF Responsibilities:**

1. Session management (Redis) - simple get/set
2. JWT enrichment (on-demand) - single service call
3. Request aggregation - parallel HTTP calls
4. Cookie management - middleware

**What BFF needs:**

- ✅ Error handling (already has ProperErrorHandling)
- ✅ Caching (already has Redis)
- ❌ JWT enrichment middleware (need to add)
- ❌ Multi-org session structure (need to update)
- ❌ Permission checking (call Permission Service)

---

### 📋 Implementation Checklist

#### Week 1: Complete Identity + Dev Mode 1

- [ ] Day 1: Fix ServiceBus publisher resource leak
- [ ] Day 1: Create Kafka publisher implementation
- [ ] Day 1: Create Kafka consumer base class
- [ ] Day 2: Update Identity Worker with Kafka support
- [ ] Day 2: Add example consumer in Identity Worker
- [ ] Day 3: Document Mode 1 (Inner Loop)
- [ ] Day 3: Create quick-start scripts
- [ ] Day 4: Write integration tests for outbox + Kafka
- [ ] Day 5: Test end-to-end event flow
- [ ] Day 5: Update documentation with real examples

#### Week 2: Dev Mode 2 + BFF Prep

- [ ] Day 1: Create docker-compose for Application Loop
- [ ] Day 2: Add Makefile commands
- [ ] Day 2: Test multi-service debugging
- [ ] Day 3: Document Mode 2
- [ ] Day 4: Analyze Landlord BFF requirements
- [ ] Day 5: Design JWT enrichment approach

---

## 🚀 Quick Wins (Do These First)

### 1. Fix ServiceBus Publisher (15 minutes)

Just add `await using` - simple fix

### 2. Create Kafka Package (2 hours)

- Create `ProperTea.ProperIntegrationEvents.Kafka` project
- Add Confluent.Kafka package
- Implement publisher
- Add registration extensions

### 3. Update Identity Worker (1 hour)

- Add Kafka configuration
- Add environment-based selection
- Test locally

### 4. Document Current Mode 1 (30 minutes)

- Write step-by-step guide
- Create run scripts
- Test with fresh setup

---

## 📊 Success Metrics

### Identity Complete When:

- ✅ ServiceBus publisher doesn't leak resources
- ✅ Kafka publisher works locally
- ✅ Worker publishes to Kafka
- ✅ Example consumer receives events
- ✅ Integration tests pass
- ✅ Mode 1 documented and working

### Ready for Phase 1b When:

- ✅ Event flow proven end-to-end
- ✅ Development workflow smooth
- ✅ Pattern documented and reusable
- ✅ Team can develop services independently

---

## 🎯 Final Recommendation

**Start with Quick Wins:**

1. Fix ServiceBus publisher (15 min) ✅
2. Create Kafka implementation (2 hrs) ✅
3. Update Identity Worker (1 hr) ✅
4. Test locally with Kafka (1 hr) ✅

**Then decide:**

- If everything works → Continue with Contact Service
- If issues found → Complete integration tests first

**Timeline:**

- Quick wins: **Today** (4-5 hours)
- Integration tests: **Tomorrow** (1 day)
- Ready for Contact Service: **Day 3**

---

**Status:** Ready to execute - start with ServiceBus fix! 🚀

