using FluentValidation;
using Marten;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public static class DeactivateHandler
{
    public static async Task Handle(
        DeactivateCommand command,
        IDocumentSession session,
        ILogger logger)
    {
        var org =
            await session.Events.AggregateStreamAsync<OrganizationAggregate>(command.OrganizationId)
            ?? throw new NotFoundException(nameof(OrganizationAggregate), command.OrganizationId);

        var deactivated = org.Deactivate(command.Reason);

        _ = session.Events.Append(command.OrganizationId, deactivated);
        await session.SaveChangesAsync(command.CancellationToken);

        logger.LogInformation(
            "Deactivated organization: {OrgId} - Reason: {Reason}",
            command.OrganizationId,
            command.Reason);
    }
}

public record DeactivateCommand(
    Guid OrganizationId,
    string Reason,
    CancellationToken CancellationToken = default);

public class DeactivateCommandValidator : AbstractValidator<DeactivateCommand>
{
    public DeactivateCommandValidator()
    {
        _ = RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Deactivation reason is required")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
