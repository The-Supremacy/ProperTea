namespace ProperTea.User.Features.UserProfiles.Infrastructure;

/// <summary>
/// Interface for external user identity provider operations.
/// Implement this for integration with external auth systems (Zitadel, Auth0, etc.).
/// </summary>
public interface IExternalUserClient
{
    // TODO: Add methods as needed for external user operations
    // Examples:
    // Task<bool> UserExistsAsync(string externalUserId, CancellationToken ct);
    // Task<ExternalUserInfo?> GetUserInfoAsync(string externalUserId, CancellationToken ct);
    // Task UpdateUserMetadataAsync(string externalUserId, Dictionary<string, string> metadata, CancellationToken ct);
}
