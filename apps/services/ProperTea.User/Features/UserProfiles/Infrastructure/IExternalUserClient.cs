namespace ProperTea.User.Features.UserProfiles.Infrastructure;

public record ExternalUserDetails(string Id, string Email, string? FirstName, string? LastName)
{
    public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Email
        : $"{FirstName} {LastName}".Trim();
}

public interface IExternalUserClient
{
    public Task<ExternalUserDetails?> GetUserDetailsAsync(string externalUserId, CancellationToken ct = default);
}
