using Microsoft.AspNetCore.Mvc;
using ProperTea.ProperSagas;
using Examples.Sagas;

namespace Examples.Endpoints;

/// <summary>
/// Example endpoints demonstrating pre-validation and saga execution
/// </summary>
public static class GDPREndpointsV2
{
    public static void MapGDPREndpointsV2(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/gdpr").WithTags("GDPR V2");

        // PRE-VALIDATE before starting saga (front-end can call this)
        group.MapPost("/delete-request/validate", ValidateDeletionRequest)
            .WithName("ValidateGDPRDeletion")
            .WithSummary("Pre-validate GDPR deletion (front-end validation)")
            .Produces<ValidationResponse>(StatusCodes.Status200OK)
            .Produces<ValidationResponse>(StatusCodes.Status400BadRequest);

        // Start actual deletion saga (after validation passes)
        group.MapPost("/delete-request", StartDeletionRequest)
            .WithName("StartGDPRDeletionV2")
            .WithSummary("Start GDPR data deletion saga")
            .Produces<GDPRDeletionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Get status
        group.MapGet("/delete-request/{sagaId:guid}", GetDeletionStatus)
            .WithName("GetGDPRDeletionStatusV2")
            .WithSummary("Get status of a GDPR deletion request")
            .Produces<GDPRDeletionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Front-end validation endpoint - runs ONLY pre-validation steps
    /// This allows UI to show validation errors BEFORE user confirms deletion
    /// </summary>
    private static async Task<IResult> ValidateDeletionRequest(
        [FromBody] GDPRDeletionRequest request,
        [FromServices] GDPRDeletionOrchestratorV2 orchestrator)
    {
        if (request.UserId == Guid.Empty || request.OrganizationId == Guid.Empty)
            return Results.BadRequest(new ValidationResponse
            {
                IsValid = false,
                ErrorMessage = "UserId and OrganizationId are required"
            });

        // Create saga for validation (don't save it yet)
        var saga = new GDPRDeletionSagaV2();
        saga.SetUserId(request.UserId);
        saga.SetOrganizationId(request.OrganizationId);

        // Run ONLY pre-validation steps
        var (isValid, errorMessage) = await orchestrator.ValidateAsync(saga);

        if (!isValid)
        {
            return Results.BadRequest(new ValidationResponse
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                FailedValidations = saga.GetPreValidationSteps()
                    .Where(s => s.Status == SagaStepStatus.Failed)
                    .Select(s => new FailedValidation
                    {
                        StepName = s.Name,
                        ErrorMessage = s.ErrorMessage ?? "Unknown error"
                    })
                    .ToList()
            });
        }

        return Results.Ok(new ValidationResponse
        {
            IsValid = true,
            Message = "All pre-validation checks passed. You can proceed with deletion."
        });
    }

    /// <summary>
    /// Start the actual deletion saga (typically called after validation passes)
    /// </summary>
    private static async Task<IResult> StartDeletionRequest(
        [FromBody] GDPRDeletionRequest request,
        [FromServices] GDPRDeletionOrchestratorV2 orchestrator)
    {
        if (request.UserId == Guid.Empty || request.OrganizationId == Guid.Empty)
            return Results.BadRequest(new { error = "UserId and OrganizationId are required" });

        // Create and start saga
        var saga = new GDPRDeletionSagaV2();
        saga.SetUserId(request.UserId);
        saga.SetOrganizationId(request.OrganizationId);

        var result = await orchestrator.StartAsync(saga);

        return result.Status switch
        {
            SagaStatus.Completed => Results.Ok(new GDPRDeletionResponse
            {
                SagaId = result.Id,
                Status = result.Status.ToString(),
                Message = "User data deletion completed successfully"
            }),
            
            SagaStatus.Failed => Results.BadRequest(new
            {
                sagaId = result.Id,
                status = result.Status.ToString(),
                error = result.ErrorMessage ?? "Deletion failed",
                failedSteps = result.Steps
                    .Where(s => s.Status == SagaStepStatus.Failed)
                    .Select(s => new { s.Name, s.ErrorMessage })
            }),
            
            SagaStatus.Compensated => Results.BadRequest(new
            {
                sagaId = result.Id,
                status = result.Status.ToString(),
                error = "Deletion failed and was rolled back",
                message = result.ErrorMessage
            }),
            
            _ => Results.Accepted($"/api/v2/gdpr/delete-request/{result.Id}", new
            {
                sagaId = result.Id,
                status = result.Status.ToString(),
                message = "Deletion is in progress"
            })
        };
    }

    private static async Task<IResult> GetDeletionStatus(
        Guid sagaId,
        [FromServices] ISagaRepository repository)
    {
        var saga = await repository.GetByIdAsync<GDPRDeletionSagaV2>(sagaId);
        if (saga == null)
            return Results.NotFound();

        return Results.Ok(new GDPRDeletionStatusResponse
        {
            SagaId = saga.Id,
            Status = saga.Status.ToString(),
            CreatedAt = saga.CreatedAt,
            CompletedAt = saga.CompletedAt,
            ErrorMessage = saga.ErrorMessage,
            PreValidationSteps = saga.GetPreValidationSteps()
                .Select(s => new StepInfo
                {
                    Name = s.Name,
                    Status = s.Status.ToString(),
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt,
                    ErrorMessage = s.ErrorMessage
                })
                .ToList(),
            ExecutionSteps = saga.GetExecutionSteps()
                .Select(s => new StepInfo
                {
                    Name = s.Name,
                    Status = s.Status.ToString(),
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt,
                    ErrorMessage = s.ErrorMessage,
                    HasCompensation = s.HasCompensation,
                    CompensationName = s.CompensationName
                })
                .ToList()
        });
    }
}

// DTOs
public record GDPRDeletionRequest(Guid UserId, Guid OrganizationId);

public record ValidationResponse
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Message { get; init; }
    public List<FailedValidation> FailedValidations { get; init; } = new();
}

public record FailedValidation
{
    public string StepName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

public record GDPRDeletionResponse
{
    public Guid SagaId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record GDPRDeletionStatusResponse
{
    public Guid SagaId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public List<StepInfo> PreValidationSteps { get; init; } = new();
    public List<StepInfo> ExecutionSteps { get; init; } = new();
}

public record StepInfo
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public bool HasCompensation { get; init; }
    public string? CompensationName { get; init; }
}

