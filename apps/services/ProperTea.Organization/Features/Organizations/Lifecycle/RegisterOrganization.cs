using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Organization.Infrastructure;

namespace ProperTea.Organization.Features.Organizations.Lifecycle;

public record RegisterOrganizationCommand(
    string OrganizationName,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string UserPassword);

public class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationValidator()
    {
        _ = RuleFor(x => x.OrganizationName)
            .NotEmpty().WithErrorCode(OrganizationErrorCodes.VALIDATION_NAME_REQUIRED)
            .MinimumLength(2).WithErrorCode(OrganizationErrorCodes.VALIDATION_NAME_TOO_SHORT)
            .MaximumLength(100).WithErrorCode(OrganizationErrorCodes.VALIDATION_NAME_TOO_LONG);

        _ = RuleFor(x => x.UserEmail)
            .NotEmpty().WithErrorCode(OrganizationErrorCodes.VALIDATION_EMAIL_REQUIRED)
            .EmailAddress().WithErrorCode(OrganizationErrorCodes.VALIDATION_EMAIL_INVALID);

        _ = RuleFor(x => x.UserFirstName)
            .NotEmpty().WithErrorCode(OrganizationErrorCodes.VALIDATION_FIRST_NAME_REQUIRED)
            .MinimumLength(1).WithErrorCode(OrganizationErrorCodes.VALIDATION_FIRST_NAME_TOO_SHORT)
            .MaximumLength(100).WithErrorCode(OrganizationErrorCodes.VALIDATION_FIRST_NAME_TOO_LONG);

        _ = RuleFor(x => x.UserLastName)
            .NotEmpty().WithErrorCode(OrganizationErrorCodes.VALIDATION_LAST_NAME_REQUIRED)
            .MinimumLength(1).WithErrorCode(OrganizationErrorCodes.VALIDATION_LAST_NAME_TOO_SHORT)
            .MaximumLength(100).WithErrorCode(OrganizationErrorCodes.VALIDATION_LAST_NAME_TOO_LONG);

        _ = RuleFor(x => x.UserPassword)
            .NotEmpty().WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_REQUIRED)
            .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_TOO_SHORT)
            .MaximumLength(100)
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_TOO_LONG)
            .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter")
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_MISSING_LOWERCASE)
            .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter")
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_MISSING_UPPERCASE)
            .Matches(@"\d")
                .WithMessage("Password must contain at least one number")
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_MISSING_NUMBER)
            .Matches(@"[^A-Za-z0-9]")
                .WithMessage("Password must contain at least one special character")
                .WithErrorCode(OrganizationErrorCodes.VALIDATION_PASSWORD_MISSING_SPECIAL);
    }
}

public record RegistrationResult(string OrganizationId, bool IsSuccess, string? Reason);

public class RegisterOrganizationHandler : IWolverineHandler
{
    public async Task<(OrganizationEvents.OrganizationRegistered, RegistrationResult, OrganizationIntegrationEvents.OrganizationRegistered)> Handle(
        RegisterOrganizationCommand command,
        IDocumentSession session,
        IExternalOrganizationClient externalOrgClient,
        ILogger logger,
        CancellationToken ct)
    {
        var exists = await externalOrgClient.CheckOrganizationExistsAsync(command.OrganizationName, ct);
        if (exists)
            throw new ConflictException(
                OrganizationErrorCodes.NAME_ALREADY_EXISTS,
                $"Organization with name '{command.OrganizationName}' already exists",
                new Dictionary<string, object> { ["organizationName"] = command.OrganizationName });

        var externalOrgId = await externalOrgClient.CreateOrganizationWithAdminAsync(
            command.OrganizationName,
            command.UserEmail,
            command.UserFirstName,
            command.UserLastName,
            command.UserPassword,
            ct);

        var streamId = Guid.NewGuid();
        var events = new List<object>
        {
            OrganizationAggregate.Create(streamId),
            OrganizationAggregate.LinkExternalOrganization(streamId, externalOrgId),
            new OrganizationEvents.Activated(streamId, DateTime.UtcNow)
        };
        _ = session.Events.StartStream<OrganizationAggregate>(streamId, [.. events]);
        await session.SaveChangesAsync(ct);

        logger.LogInformation("Registered new organization {StreamId} '{Name}' with Organization ID {OrganizationId}",
            streamId,
            command.OrganizationName,
            externalOrgId);

        var integrationEvent = new OrganizationIntegrationEvents.OrganizationRegistered(
            externalOrgId,
            command.OrganizationName,
            DateTimeOffset.UtcNow
        );

        return (
            new OrganizationEvents.OrganizationRegistered(streamId),
            new RegistrationResult(externalOrgId, IsSuccess: true, Reason: null),
            integrationEvent
        );
    }
}
