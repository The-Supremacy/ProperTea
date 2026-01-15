using FluentValidation;
using Marten;
using ProperTea.Organization.Features.Organizations.Infrastructure;
using ProperTea.ServiceDefaults.Exceptions;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public static class UpdateIdentityHandler
{
    public static async Task Handle(
        UpdateIdentityCommand command,
        IDocumentSession session,
        IZitadelClient zitadelClient,
        ILogger logger)
    {
        var org = await session.Events.AggregateStreamAsync<OrganizationAggregate>(command.OrganizationId)
            ?? throw new NotFoundException(nameof(OrganizationAggregate), command.OrganizationId);

        if (!string.IsNullOrWhiteSpace(command.NewName) && command.NewName != org.Name)
        {
            var nameExists = await session.Query<OrganizationAggregate>()
                .AnyAsync(x => x.Name == command.NewName && x.Id != command.OrganizationId);

            if (nameExists)
            {
                throw new ConflictException($"Organization name '{command.NewName}' already exists");
            }
        }

        if (!string.IsNullOrWhiteSpace(command.NewSlug) && command.NewSlug != org.Slug)
        {
            var slugExists = await session.Query<OrganizationAggregate>()
                .AnyAsync(x => x.Slug == command.NewSlug && x.Id != command.OrganizationId);

            if (slugExists)
            {
                throw new ConflictException($"Organization slug '{command.NewSlug}' already exists");
            }
        }

        var events = new List<object>();
        if (!string.IsNullOrWhiteSpace(command.NewName) && command.NewName != org.Name)
        {
            if (string.IsNullOrWhiteSpace(org.ZitadelOrganizationId))
            {
                throw new BusinessRuleViolationException("Organization is not linked to ZITADEL");
            }

            logger.LogInformation(
                "Updating organization name in ZITADEL: {OrgId} → {NewName}",
                org.ZitadelOrganizationId,
                command.NewName);

            await zitadelClient.UpdateOrganizationAsync(
                org.ZitadelOrganizationId,
                command.NewName,
                command.CancellationToken);

            var nameChanged = org.Rename(command.NewName);
            events.Add(nameChanged);
        }
        if (!string.IsNullOrWhiteSpace(command.NewSlug) && command.NewSlug != org.Slug)
        {
            var slugChanged = org.ChangeSlug(command.NewSlug);
            events.Add(slugChanged);
        }

        if (events.Count > 0)
        {
            _ = session.Events.Append(command.OrganizationId, [.. events]);
            await session.SaveChangesAsync(command.CancellationToken);

            logger.LogInformation(
                "Updated organization identity: {OrgId} ({EventCount} events)",
                command.OrganizationId,
                events.Count);
        }
    }
}

public record UpdateIdentityCommand(
    Guid OrganizationId,
    string? NewName,
    string? NewSlug,
    CancellationToken CancellationToken = default);

public class UpdateIdentityCommandValidator : AbstractValidator<UpdateIdentityCommand>
{
    public UpdateIdentityCommandValidator()
    {
        _ = RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.NewName) || !string.IsNullOrWhiteSpace(x.NewSlug))
            .WithMessage("At least one of NewName or NewSlug must be provided");

        _ = When(x => !string.IsNullOrWhiteSpace(x.NewName), () =>
        {
            _ = RuleFor(x => x.NewName)
                .MinimumLength(3).WithMessage("Organization name must be at least 3 characters")
                .MaximumLength(100).WithMessage("Organization name cannot exceed 100 characters");
        });

        _ = When(x => !string.IsNullOrWhiteSpace(x.NewSlug), () =>
        {
            _ = RuleFor(x => x.NewSlug)
                .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
                .MaximumLength(50).WithMessage("Slug cannot exceed 50 characters")
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");
        });
    }
}
