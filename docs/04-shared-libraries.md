# Shared Libraries Guide

**Version:** 1.1.0  
**Last Updated:** October 30, 2025  
**Status:** MVP 1 Reference - Revised

---

## Table of Contents

1. [Overview](#overview)
2. [ProperTea.ProperCqrs](#properteapropercqrs)
3. [ProperTea.ProperDdd](#properteaproperddd)
4. [ProperTea.ProperIntegrationEvents](#properteaproperintegrationevents)
5. [ProperTea.ProperSagas](#properteapropersagas)
6. [ProperTea.ProperStorage](#properteaproperstorage)
7. [ProperTea.ProperTelemetry](#properteapropertelemetry)
8. [ProperTea.ProperErrorHandling](#properteapropererrorhandling)

---

## Overview

ProperTea uses custom shared libraries to implement cross-cutting patterns without dependency on commercial products
like MediatR or MassTransit.

### Design Philosophy

- **Educational First:** Learn patterns by implementing them
- **Lightweight:** Minimal dependencies, simple implementations
- **Flexible:** Easy to customize for specific needs
- **Testable:** Built with testing in mind

### Library Dependencies

```
ProperCqrs (independent)
ProperDdd (independent)
ProperIntegrationEvents (uses ProperDdd for Outbox)
ProperSagas (extends ProperDdd)
ProperStorage (independent)
ProperTelemetry (independent)
ProperErrorHandling (independent)
```

---

## ProperTea.ProperCqrs

**Purpose:** Command Query Responsibility Segregation pattern implementation.

**NuGet Package:** `ProperTea.ProperCqrs`

### Installation

```bash
dotnet add package ProperTea.ProperCqrs
```

### Core Concepts

**Commands** - Change system state, return results
**Queries** - Read system state, never modify

### Usage Example

**1. Define Command:**

```csharp
using ProperTea.ProperCqrs;

public record CreatePropertyCommand : ICommand<Guid>
{
    public Guid CompanyId { get; init; }
    public string Name { get; init; }
    public string Address { get; init; }
}
```

**2. Create Command Handler:**

```csharp
public class CreatePropertyCommandHandler : ICommandHandler<CreatePropertyCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Guid> HandleAsync(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var property = new Property(command.CompanyId, command.Name, command.Address);
        
        await _repository.AddAsync(property, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        
        return property.Id;
    }
}
```

**3. Define Query:**

```csharp
public record GetPropertiesQuery : IQuery<List<PropertyDto>>
{
    public Guid CompanyId { get; init; }
    public string? SearchTerm { get; init; }
}
```

**4. Create Query Handler:**

```csharp
public class GetPropertiesQueryHandler : IQueryHandler<GetPropertiesQuery, List<PropertyDto>>
{
    private readonly ProperTeaDbContext _context;

    public async Task<List<PropertyDto>> HandleAsync(GetPropertiesQuery query, CancellationToken cancellationToken)
    {
        var propertiesQuery = _context.Properties
            .Where(p => p.CompanyId == query.CompanyId);

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            propertiesQuery = propertiesQuery.Where(p => p.Name.Contains(query.SearchTerm));
        }

        return await propertiesQuery
            .Select(p => new PropertyDto(p.Id, p.Name, p.Address))
            .ToListAsync(cancellationToken);
    }
}
```

**5. Register in Program.cs:**

```csharp
builder.Services.AddProperCqrs(typeof(Program).Assembly);
```

**6. Use in Endpoints:**

```csharp
app.MapPost("/api/properties", async (
    CreatePropertyCommand command,
    ICommandBus commandBus) =>
{
    var propertyId = await commandBus.SendAsync(command);
    return Results.Created($"/api/properties/{propertyId}", new { id = propertyId });
});

app.MapGet("/api/properties", async (
    [AsParameters] GetPropertiesQuery query,
    IQueryBus queryBus) =>
{
    var properties = await queryBus.SendAsync(query);
    return Results.Ok(properties);
});
```

### Validation

**ProperCqrs includes FluentValidation integration:**

```csharp
public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}
```

**Validation happens automatically via decorator pattern:**

```csharp
// In ServiceCollectionExtensions
services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidationQueryHandlerDecorator<,>));
```

---

## ProperTea.ProperDdd

**Purpose:** Domain-Driven Design building blocks.

**NuGet Package:** `ProperTea.ProperDdd`

### Core Classes

**Aggregate Root:**

```csharp
using ProperTea.ProperDdd;

public class Property : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; }
    
    private readonly List<Building> _buildings = new();
    public IReadOnlyCollection<Building> Buildings => _buildings.AsReadOnly();

    public Property(Guid companyId, string name, string address)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name;
        
        AddDomainEvent(new PropertyCreatedEvent(Id, companyId, name));
    }

    public void AddBuilding(string buildingName)
    {
        var building = new Building(Id, buildingName);
        _buildings.Add(building);
        
        AddDomainEvent(new BuildingAddedEvent(Id, building.Id));
    }
}
```

**Entity:**

```csharp
public class Building : Entity
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }

    internal Building(Guid propertyId, string name)
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        Name = name;
    }
}
```

**Value Object:**

```csharp
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }

    public Address(string street, string city, string postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}
```

**Domain Events:**

```csharp
public record PropertyCreatedEvent(Guid PropertyId, Guid CompanyId, string Name) : DomainEvent;
public record BuildingAddedEvent(Guid PropertyId, Guid BuildingId) : DomainEvent;
```

### Repository Pattern

```csharp
public interface IRepository<TAggregate> where TAggregate : AggregateRoot
{
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
```

### Unit of Work

```csharp
// Usage
public class CreatePropertyCommandHandler : ICommandHandler<CreatePropertyCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Guid> HandleAsync(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var property = new Property(command.CompanyId, command.Name, command.Address);
        
        await _repository.AddAsync(property, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // Saves entity + publishes domain events to outbox
        
        return property.Id;
    }
}
```

### Registration

```csharp
// Program.cs
builder.Services.AddProperDdd()
    .UseEntityFramework<PropertyDbContext>();
```

---

## ProperTea.ProperIntegrationEvents

**Purpose:** A lightweight framework for event-driven communication between services, featuring a robust outbox pattern
and handlers for converting domain events to integration events.

**NuGet Packages:**

- `ProperTea.ProperIntegrationEvents` (core)
- `ProperTea.ProperIntegrationEvents.Outbox` (outbox pattern)
- `ProperTea.ProperIntegrationEvents.Outbox.Ef` (EF Core storage)
- `ProperTea.ProperIntegrationEvents.ServiceBus` (Azure Service Bus)
- `ProperTea.ProperIntegrationEvents.Kafka` (Kafka)

### Define Integration Event

```csharp
using ProperTea.ProperIntegrationEvents;

public class PropertyCreatedEvent : IntegrationEventBase
{
    public Guid PropertyId { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string EventType => "PropertyCreated";
    public override string Topic => "property-events";
}
```

### Publisher: Converting Domain Events to Integration Events

The recommended pattern is to use a dedicated handler that subscribes to a domain event and is responsible for creating
and publishing the corresponding integration event. This decouples your core domain logic from the specifics of
integration contracts.

**1. Define a Domain Event:**
This happens within your domain model (`ProperTea.ProperDdd`).

```csharp
// In Property.Domain/Events
public record PropertyCreatedDomainEvent(Guid PropertyId, Guid CompanyId, string Name) : IDomainEvent;
```

**2. Define the corresponding Integration Event:**
This is the public contract for other services.

```csharp
// In Property.Service/IntegrationEvents
public class PropertyCreatedIntegrationEvent : IntegrationEvent
{
    public Guid PropertyId { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**3. Create a `DomainEventToIntegrationEventProcessor`:**
This handler listens for the domain event and publishes the integration event.

```csharp
using ProperTea.ProperDdd.DomainEvents;
using ProperTea.ProperIntegrationEvents;

// This handler lives in your service's application layer.
public class PropertyCreatedDomainEventHandler : IDomainEventHandler<PropertyCreatedDomainEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public PropertyCreatedDomainEventHandler(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(PropertyCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var integrationEvent = new PropertyCreatedIntegrationEvent
        {
            PropertyId = domainEvent.PropertyId,
            CompanyId = domainEvent.CompanyId,
            Name = domainEvent.Name
        };

        // This will save the event to the outbox within the same transaction.
        await _publisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

**4. Register the Handler:**
The `ProperDdd` library will automatically discover and invoke this handler when `_unitOfWork.CommitAsync()` is called.

**Benefits of this pattern:**

- ✅ **Decoupling:** Your domain model is completely unaware of integration events.
- ✅ **SOLID:** Each mapping has its own class, adhering to the Single Responsibility Principle.
- ✅ **Testable:** The mapping logic can be easily unit-tested.

### The Outbox Pattern (How it Works)

When `_publisher.PublishAsync()` is called with an outbox configured, the following happens:

1. The `integrationEvent` is serialized to JSON.
2. A new `OutboxMessage` is created with the payload, topic, and a `Pending` status.
3. This `OutboxMessage` is saved to the database as part of the **same transaction** that saves your business data (
   e.g., the new `Property` aggregate).
4. A background service (`OutboxProcessor`) periodically queries the database for `Pending` messages, publishes them to
   the message broker (e.g., Kafka), and marks them as `Published`.

This guarantees that an integration event is only published if the original database transaction succeeds.

### Consumer (Worker)

The consumer side needs to know how to deserialize the JSON payload from an incoming message back into a specific
`IntegrationEvent` type. This requires registering all expected event types.

**1. Register Event Types in Worker `Program.cs`:**

```csharp
builder.Services.AddProperIntegrationEvents()
    .UseKafka(builder.Configuration.GetConnectionString("Kafka")!)
    .AddEventType<PropertyCreatedIntegrationEvent>("PropertyCreated"); // Register the event
    // Add more event types here
    // .AddEventType<PropertyUpdatedIntegrationEvent>("PropertyUpdated");

builder.Services.AddHostedService<PropertyEventConsumer>();
```

This registration builds a map of `eventTypeName` strings to .NET `Type` objects, which the `OutboxProcessor` uses for
deserialization.

**2. Create Consumer:**

```csharp
public class PropertyEventConsumer : BackgroundService
{
    private readonly IIntegrationEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;

    public PropertyEventConsumer(IIntegrationEventBus eventBus, IServiceProvider serviceProvider)
    {
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The event bus uses the registered event types to deserialize messages
        await _eventBus.SubscribeAsync<PropertyCreatedIntegrationEvent>(async (@event, ct) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<PropertyCreatedHandler>();
            await handler.HandleAsync(@event, ct);
        }, stoppingToken);
    }
}

// The handler that performs the business logic
public class PropertyCreatedHandler
{
    private readonly IRentalManagementService _rentalService;

    public async Task HandleAsync(PropertyCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // React to property creation by creating a RentalObject
        await _rentalService.CreateRentalObjectForUnitAsync(@event.PropertyId, cancellationToken);
    }
}
```

---

## ProperTea.ProperSagas

**Purpose:** Orchestration for multi-step workflows with automatic compensation on failure.

**NuGet Packages:**

- `ProperTea.ProperSagas` (core abstractions)
- `ProperTea.ProperSagas.Ef` (Entity Framework Core persistence)

### When to Use Sagas

✅ **Use for:**

- Multi-step workflows requiring coordination across services
- Processes that need rollback/compensation on failure
- Long-running operations that may be paused and resumed
- Critical business processes with multiple validation steps

❌ **Don't use for:**

- Simple event reactions (use choreography with `ProperIntegrationEvents`)
- Single-service operations
- Fire-and-forget notifications

### Core Components

1. **`SagaBase`** - Base class for saga state with step tracking and strongly-typed data storage
2. **`SagaOrchestratorBase<TSaga>`** - Abstract orchestrator that executes steps with automatic compensation
3. **`ISagaRepository`** - Interface for saga persistence
4. **`EfSagaRepository<TContext>`** - Generic EF Core implementation (in ProperSagas.Ef package)
5. **Optional: `BackgroundService`** - For polling and resuming long-running sagas

### Key Features

- ✅ **Step-by-step execution** - Each step is persisted, saga can be resumed after crash
- ✅ **Automatic compensation** - Failed steps trigger rollback in reverse order
- ✅ **Strongly-typed data** - `SetData<T>()` / `GetData<T>()` for type-safe saga data storage
- ✅ **Resume capability** - `ResumeAsync()` continues saga from last completed step
- ✅ **Query by status** - Find waiting sagas for background processing
- ✅ **EF Core package** - No need to implement repository in each service

### Quick Start

**1. Install the package:**

```bash
dotnet add reference ../../../services/Shared/ProperTea.ProperSagas.Ef/ProperTea.ProperSagas.Ef.csproj
```

**2. Add SagaEntity to your DbContext:**

```csharp
using ProperTea.ProperSagas.Ef;

public class YourServiceDbContext : DbContext
{
    public DbSet<SagaEntity> Sagas { get; set; }
}
```

**3. Create migration:**

```bash
dotnet ef migrations add AddSagaSupport
dotnet ef database update
```

**4. Register in DI:**

```csharp
// Program.cs
builder.Services.AddProperSagasEf<YourServiceDbContext>();
builder.Services.AddScoped<YourOrchestrator>();

// Optional: Add background processor for long-running sagas
builder.Services.AddHostedService<SagaProcessor>();
```

**5. Create your saga and orchestrator:**

See complete working examples in `/docs/examples/sagas/`:

- `GDPRDeletionSaga.cs` - Example saga with strongly-typed helpers
- `GDPRDeletionOrchestrator.cs` - Complete orchestrator with validation and compensation
- `SagaProcessor.cs` - Background service for polling waiting sagas
- `GDPREndpoints.cs` - API endpoints for saga management

### Core API

**SagaBase Methods:**

```csharp
// Strongly-typed data storage
saga.SetData<T>("key", value);
T value = saga.GetData<T>("key");
bool exists = saga.HasData("key");

// Status management
saga.MarkAsRunning();
saga.MarkAsCompleted();
saga.MarkAsFailed("error message");
saga.MarkAsWaitingForCallback("waiting for approval");
saga.MarkAsCompensating();
saga.MarkAsCompensated();
```

**SagaOrchestratorBase Methods:**

```csharp
// Start new saga
var result = await orchestrator.StartAsync(saga);

// Resume existing saga (after crash or callback)
var saga = await orchestrator.ResumeAsync(sagaId);

// Execute step with automatic error handling and persistence
bool success = await ExecuteStepAsync(saga, "StepName", async () =>
{
    // Your step logic here
});
```

**ISagaRepository Methods:**

```csharp
// Provided by ProperSagas.Ef package
var saga = await repository.GetByIdAsync<YourSaga>(sagaId);
await repository.SaveAsync(saga);
await repository.UpdateAsync(saga);
List<Guid> waitingSagas = await repository.FindByStatusAsync(SagaStatus.WaitingForCallback);
```

### Implementation Pattern

**Typical saga flow:**

```csharp
protected override async Task ExecuteStepsAsync(YourSaga saga)
{
    // 1. Validation Phase (read-only, no compensation needed)
    if (!await ExecuteStepAsync(saga, "ValidateStep", async () =>
    {
        if (await _service.HasBlockingConditionAsync())
            throw new InvalidOperationException("Cannot proceed");
    }))
    {
        saga.MarkAsFailed("Validation failed");
        return; // No compensation for validation failures
    }

    // 2. Execution Phase (writes data, needs compensation)
    if (!await ExecuteStepAsync(saga, "ExecuteStep1", async () =>
    {
        var result = await _service.DoSomethingAsync();
        saga.SetData("result", result); // Store for compensation
    }))
    {
        await CompensateAsync(saga); // Trigger rollback
        return;
    }

    // More steps...

    saga.MarkAsCompleted();
}

protected override async Task CompensateAsync(YourSaga saga)
{
    // Rollback completed steps in REVERSE order
    var completedSteps = saga.Steps
        .Where(s => s.Status == SagaStepStatus.Completed)
        .Reverse();

    foreach (var step in completedSteps)
    {
        // Undo each step based on its name
    }
}
```

### Complete Examples

See `/docs/examples/sagas/` for:

- ✅ Complete GDPR deletion saga implementation
- ✅ Validation and execution phases
- ✅ Compensation logic
- ✅ Background processor for long-running sagas
- ✅ API endpoints for saga management
- ✅ Strongly-typed data helpers

### Reference Documentation

- **Examples:** `/docs/examples/sagas/` - Complete working code
- **Quick Reference:** `/docs/QUICK-REFERENCE.md` - Pattern comparison
- **Concepts:** `/docs/03-event-driven-patterns.md` - When to use sagas
- **EF Package:** `/services/Shared/ProperTea.ProperSagas.Ef/README.md` - Package details

---

## ProperTea.ProperStorage

**Purpose:** Abstraction for blob storage (Azurite locally, Azure Blob in production).

**NuGet Package:** `ProperTea.ProperStorage`

### Configuration

```csharp
// Program.cs
builder.Services.AddProperBlobStorage(builder.Configuration);
```

```json
// appsettings.Development.json
{
  "BlobStorage": {
    "Provider": "Azurite",
    "Azurite": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
}

// appsettings.Production.json
{
  "BlobStorage": {
    "Provider": "AzureBlob",
    "AzureBlob": {
      "ConnectionString": "from-keyvault"
    }
  }
}
```

### Usage

```csharp
public class LeaseService
{
    private readonly IBlobStorageService _blobStorage;

    public async Task<string> UploadSignedLeaseAsync(Guid leaseId, Stream pdfStream)
    {
        var blobName = $"leases/{leaseId}/signed-agreement.pdf";
        var url = await _blobStorage.UploadAsync(
            containerName: "legal-documents",
            blobName: blobName,
            content: pdfStream,
            contentType: "application/pdf"
        );
        
        return url;
    }

    public async Task<Stream> DownloadLeaseAsync(Guid leaseId)
    {
        var blobName = $"leases/{leaseId}/signed-agreement.pdf";
        return await _blobStorage.DownloadAsync("legal-documents", blobName);
    }
}
```

---

## ProperTea.ProperTelemetry

**Purpose:** OpenTelemetry configuration for traces, metrics, and logs.

**NuGet Package:** `ProperTea.ProperTelemetry`

### Configuration

```csharp
// Program.cs
var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>()!;
builder.AddProperTelemetry(otelOptions);
```

```json
{
  "OpenTelemetry": {
    "ServiceName": "ProperTea.Identity.Service",
    "Endpoint": "http://jaeger:4317",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true
  }
}
```

### Usage (Automatic Instrumentation)

**HTTP requests, database queries, and event publishing are automatically traced.**

**Custom traces:**

```csharp
public class PropertyService
{
    private static readonly ActivitySource ActivitySource = new("ProperTea.Property");

    public async Task<Property> CreatePropertyAsync(CreatePropertyCommand command)
    {
        using var activity = ActivitySource.StartActivity("CreateProperty");
        activity?.SetTag("companyId", command.CompanyId);
        activity?.SetTag("propertyName", command.Name);

        // Business logic
        var property = new Property(command.CompanyId, command.Name, command.Address);
        await _repository.AddAsync(property);

        activity?.SetTag("propertyId", property.Id);
        return property;
    }
}
```

---

## ProperTea.ProperErrorHandling

**Purpose:** Global exception handling and error responses.

**NuGet Package:** `ProperTea.ProperErrorHandling`

### Configuration

```csharp
// Program.cs
builder.AddProperGlobalErrorHandling("ProperTea.Identity.Service");
```

### Custom Exceptions

```csharp
public class PropertyNotFoundException : NotFoundException
{
    public PropertyNotFoundException(Guid propertyId) 
        : base($"Property with ID {propertyId} was not found")
    {
    }
}

public class PropertyValidationException : ValidationException
{
    public PropertyValidationException(string message) : base(message)
    {
    }
}
```

**These are automatically caught and converted to appropriate HTTP responses:**

- `NotFoundException` → 404
- `ValidationException` → 400
- `UnauthorizedException` → 401
- `ForbiddenException` → 403
- Unhandled exceptions → 500 (with details hidden in production)

---

## Testing Shared Libraries

All shared libraries have comprehensive unit tests. See `tests/services/Shared/` for examples.

**Run all tests:**

```bash
dotnet test tests/services/Shared/
```

---

**Document Version:**

| Version | Date       | Changes                                                                                         |
|---------|------------|-------------------------------------------------------------------------------------------------|
| 1.1.0   | 2025-10-30 | Adopted `DomainEventToIntegrationEventProcessor` pattern and clarified event type registration. |
| 1.0.0   | 2025-10-22 | Initial shared libraries guide                                                                  |
