using Marten;

namespace ProperTea.User.Features.UserProfiles.Domain;

/// <summary>
/// Domain service for user profile operations requiring cross-aggregate logic or external queries.
/// Use for validations that need IDocumentSession before calling aggregate methods.
/// </summary>
public class UserProfileDomainService
{
    private readonly IDocumentSession _session;

    public UserProfileDomainService(IDocumentSession session)
    {
        _session = session;
    }

    // TODO: Add domain service methods
    // Example: ValidateUniqueExternalUserIdAsync(externalUserId) - queries DB
    // Then handler calls aggregate.Create() with validated data
}
