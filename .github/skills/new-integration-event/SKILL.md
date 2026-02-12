---
name: new-integration-event
description: Wire a new cross-service integration event end-to-end in ProperTea. Use when adding a new event, message, or cross-service communication between microservices.
---

# Add a New Integration Event

You are wiring a new cross-service integration event for the ProperTea system.

## Before You Start
1. Read `/docs/event-catalog.md` to check for conflicts and understand naming conventions.
2. Read `/docs/architecture.md` to confirm which service owns this event.
3. Understand the contract-first pattern: interface in Contracts, implementation in the service.

## Steps

### 1. Define the Contract Interface

In `shared/ProperTea.Contracts/Events/{Context}IntegrationEvents.cs`:

```csharp
public interface I{Entity}{Action}
{
    public Guid {Entity}Id { get; }
    // Other read-only properties
    public DateTimeOffset {Action}At { get; }
}
```

Rules:
- Interface only. Read-only `{ get; }` properties.
- One file per bounded context. Add to existing file if context already exists.

### 2. Implement in the Publishing Service

In `Features/{FeatureName}/{Name}IntegrationEvents.cs`:

```csharp
[MessageIdentity("{entity}.{action}.v1")]
public class {Entity}{Action} : I{Entity}{Action}
{
    public Guid {Entity}Id { get; set; }
    // Match all interface properties
    public DateTimeOffset {Action}At { get; set; }
}
```

Rules:
- `[MessageIdentity]` naming: `{entity}.{action}.v{version}` (lowercase, dot-separated).
- Concrete class (not record) for integration events. Use `{ get; set; }`.

### 3. Publish from Handler

In the handler that triggers this event:

```csharp
await bus.PublishAsync(new {Entity}{Action}
{
    {Entity}Id = id,
    {Action}At = DateTimeOffset.UtcNow
});
```

Or use Wolverine's cascading return pattern for automatic publishing.

### 4. Subscribe in the Consuming Service

Create a handler in the consuming service:

```csharp
public class Handle{Entity}{Action} : IWolverineHandler
{
    public async Task Handle(I{Entity}{Action} message, IDocumentSession session)
    {
        // React to the event
    }
}
```

Note: The handler consumes the **interface** (`I{Entity}{Action}`), not the concrete type.

### 5. Configure Wolverine Transport

In the publishing service's Wolverine configuration (typically in the `WolverineMessagingExtensions` helper):

```csharp
opts.PublishMessage<{Entity}{Action}>()
    .ToRabbitTopics("{context}.events")
    .UseDurableOutbox();
```

In each consuming service, declare the topic exchange with a binding key pattern and listen to the queue:

```csharp
opts.UseRabbitMqUsingNamedConnection("rabbitmq")
    .DeclareExchange("{context}.events", exchange =>
    {
        exchange.ExchangeType = ExchangeType.Topic;
        _ = exchange.BindQueue("{consumer-service}.{context}-events", "{entities}.#");
    });

opts.ListenToRabbitQueue("{consumer-service}.{context}-events").UseDurableInbox();
```

### 6. Update Documentation

Add the new event to `/docs/event-catalog.md` in the appropriate exchange table:

```markdown
| `{entity}.{action}.v1` | `I{Entity}{Action}` | **{Handler}** -- {Description}. | **{Consumer Service}** |
```

## Checklist
- [ ] Interface defined in `ProperTea.Contracts/Events/`
- [ ] Implementation in publishing service with `[MessageIdentity]`
- [ ] Published from handler via `IMessageBus` or cascading return
- [ ] Consumer handler accepts the interface type
- [ ] RabbitMQ exchange/queue configured in Wolverine
- [ ] `/docs/event-catalog.md` updated
