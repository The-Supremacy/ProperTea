using FluentValidation;
using Marten;
using Wolverine;
using ProperTea.Contracts.Events;

namespace ProperTea.User.Features.UserProfiles.CreateUserProfile;

/// <summary>
/// Command to create a new user profile on first login
/// </summary>
public record CreateUserProfileCommand(string ZitadelUserId);

public class CreateUserProfileValidator : AbstractValidator<CreateUserProfileCommand>
{
    public CreateUserProfileValidator()
    {
        _ = RuleFor(x => x.ZitadelUserId)
            .NotEmpty().WithMessage("Zitadel user ID is required");
    }
}

/// <summary>
/// Result returned from CreateUserProfile handler (Wolverine cascade pattern)
/// </summary>
public record CreateUserProfileResult(Guid ProfileId);

/// <summary>
/// Integration event for user profile creation.
/// Implements IUserProfileCreated contract from shared Contracts.
/// Published via IMessageBus.PublishAsync() to RabbitMQ.
/// </summary>
public class UserProfileCreatedEvent(
    Guid profileId,
    string zitadelUserId,
    DateTimeOffset createdAt) : IUserProfileCreated
{
    public Guid ProfileId { get; } = profileId;
    public string ZitadelUserId { get; } = zitadelUserId;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

public static class CreateUserProfileHandler
{
    public static async Task<CreateUserProfileResult> Handle(
        CreateUserProfileCommand command,
        IDocumentSession session,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Check if profile already exists (idempotency)
        var existingProfile = await session.Query<UserProfileAggregate>()
            .FirstOrDefaultAsync(x => x.ZitadelUserId == command.ZitadelUserId, cancellationToken);

        if (existingProfile is not null)
        {
            return new CreateUserProfileResult(existingProfile.Id);
        }

        var profileId = Guid.NewGuid();
        var created = UserProfileAggregate.Create(profileId, command.ZitadelUserId);

        _ = session.Events.StartStream<UserProfileAggregate>(profileId, created);
        await session.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var integrationEvent = new UserProfileCreatedEvent(
            profileId,
            command.ZitadelUserId,
            created.CreatedAt
        );
        await messageBus.PublishAsync(integrationEvent);

        return new CreateUserProfileResult(profileId);
    }
}
