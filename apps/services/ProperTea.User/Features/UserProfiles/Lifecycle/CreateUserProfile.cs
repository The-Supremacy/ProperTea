using FluentValidation;
using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record CreateProfileCommand(string UserId);

public class CreateProfileValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileValidator()
    {
        _ = RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .WithErrorCode(UserProfileErrorCodes.VALIDATION_EXTERNAL_ID_REQUIRED);
    }
}

public record CreateProfileResult(Guid ProfileId);

public class CreateProfileHandler : IWolverineHandler
{
    public async Task<CreateProfileResult> Handle(
        CreateProfileCommand command,
        IDocumentSession session,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var existingProfile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.UserId == command.UserId, cancellationToken);

        if (existingProfile is not null)
        {
            return new CreateProfileResult(existingProfile.Id);
        }

        var profileId = Guid.NewGuid();
        var created = UserProfileAggregate.Create(profileId, command.UserId);

        _ = session.Events.StartStream<UserProfileAggregate>(profileId, created);
        await session.SaveChangesAsync(cancellationToken);

        var integrationEvent = new UserProfileIntegrationEvents.UserProfileCreatedEvent(
            command.UserId,
            created.CreatedAt
        );
        await messageBus.PublishAsync(integrationEvent);

        return new CreateProfileResult(profileId);
    }
}
