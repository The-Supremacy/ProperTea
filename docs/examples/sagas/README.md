# Saga Implementation Examples

This folder contains complete, working examples of saga implementation using ProperTea.ProperSagas.

## Files

### Basic Examples (V1)
- **GDPRDeletionSaga.cs** - Basic saga class with strongly-typed data helpers
- **GDPRDeletionOrchestrator.cs** - Basic orchestrator with validation and compensation
- **SagaProcessor.cs** - Background service for polling waiting sagas (local dev)
- **GDPREndpoints.cs** - API endpoints for starting and managing sagas

### Advanced Examples (V2) - Recommended
- **GDPRDeletionSagaV2.cs** - ✨ Saga with pre-validation steps and flexible compensation
- **GDPRDeletionOrchestratorV2.cs** - ✨ Orchestrator with validation endpoint support and auto-compensation
- **GDPREndpointsV2.cs** - ✨ API endpoints with front-end validation support

## Usage

These are **reference examples** - copy and adapt them to your specific needs.

### V1 Examples Demonstrate:
✅ Basic saga structure  
✅ Manual validation in ExecuteStepsAsync  
✅ Manual compensation logic  
✅ Strongly-typed data storage  
✅ API endpoints for saga management  

### V2 Examples Demonstrate (Recommended):
✨ **Pre-validation pattern** - Separate validation from execution  
✨ **Front-end validation** - Validate before starting saga  
✨ **Flexible compensation** - Per-step compensation configuration  
✨ **Point of no return** - Steps that cannot be compensated  
✨ **Automatic compensation** - Built-in helper for common patterns  

## How to Use

1. **Copy the files** to your service project (use V2 for new implementations)
2. **Adapt** the saga steps to your workflow
3. **Implement** the service interfaces (ILeaseService, etc.)
4. **Register** in DI:
   ```csharp
   builder.Services.AddProperSagasEf<YourDbContext>();
   builder.Services.AddScoped<GDPRDeletionOrchestratorV2>();
   builder.Services.AddHostedService<SagaProcessor>(); // Optional
   ```
5. **Add migration**:
   ```bash
   dotnet ef migrations add AddSagaSupport
   dotnet ef database update
   ```

## Front-End Integration (V2)

```typescript
// Step 1: Validate before showing confirmation
const validateResponse = await fetch('/api/v2/gdpr/delete-request/validate', {
    method: 'POST',
    body: JSON.stringify({ userId, organizationId })
});

if (!validateResponse.ok) {
    const errors = await validateResponse.json();
    // Show validation errors
    alert(`Cannot delete: ${errors.errorMessage}`);
    return;
}

// Step 2: Show confirmation dialog
if (confirm('All validation passed. Proceed with deletion?')) {
    // Step 3: Start saga
    await fetch('/api/v2/gdpr/delete-request', {
        method: 'POST',
        body: JSON.stringify({ userId, organizationId })
    });
}
```

## See Also

- **Validation & Compensation Guide:** `/docs/SAGA-VALIDATION-COMPENSATION.md`
- **Quick Reference:** `/docs/QUICK-REFERENCE.md`
- **Implementation Summary:** `/docs/IMPLEMENTATION-SUMMARY.md`

