using FluentValidation;
using Marten;
using Wolverine;

namespace ProperTea.User.Features.UserProfiles.Lifecycle;

public record CreateProfileCommand(string ExternalUserId);

public class CreateProfileValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileValidator()
    {
        _ = RuleFor(x => x.ExternalUserId)
            .NotEmpty().WithMessage("External user ID is required");
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
            .FirstOrDefaultAsync(x => x.ExternalUserId == command.ExternalUserId, cancellationToken);

        if (existingProfile is not null)
        {
            return new CreateProfileResult(existingProfile.Id);
        }

        var profileId = Guid.NewGuid();
        var created = UserProfileAggregate.Create(profileId, command.ExternalUserId);

        _ = session.Events.StartStream<UserProfileAggregate>(profileId, created);
        await session.SaveChangesAsync(cancellationToken);

        var integrationEvent = new UserProfileIntegrationEvents.UserProfileCreatedEvent(
            profileId,
            command.ExternalUserId,
            created.CreatedAt
        );
        await messageBus.PublishAsync(integrationEvent);

        return new CreateProfileResult(profileId);
    }
}
