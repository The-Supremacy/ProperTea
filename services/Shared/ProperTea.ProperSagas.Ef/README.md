# ProperTea.ProperSagas.Ef

Entity Framework Core implementation of saga persistence for `ProperTea.ProperSagas`.

## Installation

```bash
dotnet add package ProperTea.ProperSagas.Ef
```

## Usage

### 1. Add SagaEntity to your DbContext

```csharp
using ProperTea.ProperSagas.Ef;

public class MyServiceDbContext : DbContext
{
    public DbSet<SagaEntity> Sagas { get; set; }
    
    // Your other DbSets...
}
```

### 2. Create a migration

```bash
dotnet ef migrations add AddSagaSupport
dotnet ef database update
```

### 3. Register in DI

```csharp
// Program.cs
builder.Services.AddProperSagasEf<MyServiceDbContext>();
```

That's it! Now you can use `ISagaRepository` in your orchestrators.

## Example

```csharp
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    public GDPRDeletionOrchestrator(
        ISagaRepository sagaRepository,  // Auto-injected
        ILogger<GDPRDeletionOrchestrator> logger)
        : base(sagaRepository, logger)
    {
    }
    
    // Implement your orchestrator...
}
```

## Database Schema

The `SagaEntity` creates the following table:

```sql
CREATE TABLE Sagas (
    Id UUID PRIMARY KEY,
    SagaType VARCHAR(200) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    SagaData TEXT NOT NULL,      -- JSON
    Steps TEXT NOT NULL,          -- JSON array
    ErrorMessage TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CompletedAt TIMESTAMP NULL
);
```

## Features

- ✅ Automatic saga state persistence
- ✅ JSON serialization for data and steps
- ✅ Query sagas by status
- ✅ Type-safe saga retrieval
- ✅ Works with any EF Core DbContext

