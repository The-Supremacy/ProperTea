using Microsoft.AspNetCore.Mvc;
using ProperTea.ProperSagas;
using Examples.Sagas;

namespace Examples.Endpoints;

/// <summary>
/// Example endpoints for starting and managing GDPR deletion sagas
/// This is a reference example - adapt to your needs
/// </summary>
public static class GDPREndpoints
{
    public static void MapGDPREndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gdpr").WithTags("GDPR");

        // Start a GDPR deletion request
        group.MapPost("/delete-request", StartDeletionRequest)
            .WithName("StartGDPRDeletion")
            .WithSummary("Start GDPR data deletion for a user")
            .Produces<GDPRDeletionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Get status of a deletion request
        group.MapGet("/delete-request/{sagaId:guid}", GetDeletionStatus)
            .WithName("GetGDPRDeletionStatus")
            .WithSummary("Get status of a GDPR deletion request")
            .Produces<GDPRDeletionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Approve a pending deletion (if you need manual approval)
        group.MapPost("/delete-request/{sagaId:guid}/approve", ApproveDeletion)
            .WithName("ApproveGDPRDeletion")
            .WithSummary("Approve a pending GDPR deletion request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> StartDeletionRequest(
        [FromBody] GDPRDeletionRequest request,
        [FromServices] GDPRDeletionOrchestrator orchestrator,
        [FromServices] IContactService contactService)
    {
        // Validate request
        if (request.UserId == Guid.Empty || request.OrganizationId == Guid.Empty)
            return Results.BadRequest(new { error = "UserId and OrganizationId are required" });

        // Create and start saga
        var saga = new GDPRDeletionSaga();
        saga.SetUserId(request.UserId);
        saga.SetOrganizationId(request.OrganizationId);

        var result = await orchestrator.StartAsync(saga);

        return result.Status == SagaStatus.Completed
            ? Results.Ok(new GDPRDeletionResponse
            {
                SagaId = result.Id,
                Status = result.Status.ToString(),
                Message = "User data deletion completed successfully"
            })
            : Results.BadRequest(new
            {
                sagaId = result.Id,
                status = result.Status.ToString(),
                error = result.ErrorMessage ?? "Deletion failed"
            });
    }

    private static async Task<IResult> GetDeletionStatus(
        Guid sagaId,
        [FromServices] ISagaRepository repository)
    {
        var saga = await repository.GetByIdAsync<GDPRDeletionSaga>(sagaId);
        if (saga == null)
            return Results.NotFound();

        return Results.Ok(new GDPRDeletionStatusResponse
        {
            SagaId = saga.Id,
            Status = saga.Status.ToString(),
            CreatedAt = saga.CreatedAt,
            CompletedAt = saga.CompletedAt,
            ErrorMessage = saga.ErrorMessage,
            Steps = saga.Steps.Select(s => new StepInfo
            {
                Name = s.Name,
                Status = s.Status.ToString(),
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                ErrorMessage = s.ErrorMessage
            }).ToList()
        });
    }

    private static async Task<IResult> ApproveDeletion(
        Guid sagaId,
        [FromServices] GDPRDeletionOrchestrator orchestrator,
        [FromServices] ISagaRepository repository)
    {
        var saga = await repository.GetByIdAsync<GDPRDeletionSaga>(sagaId);
        if (saga == null)
            return Results.NotFound();

        if (saga.Status != SagaStatus.WaitingForCallback)
            return Results.BadRequest(new { error = "Saga is not waiting for approval" });

        // Resume the saga
        await orchestrator.ResumeAsync(sagaId);

        return Results.Ok(new { message = "Deletion request approved and resumed" });
    }
}

// Request/Response DTOs
public record GDPRDeletionRequest(Guid UserId, Guid OrganizationId);

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
    public List<StepInfo> Steps { get; init; } = new();
}

public record StepInfo
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

