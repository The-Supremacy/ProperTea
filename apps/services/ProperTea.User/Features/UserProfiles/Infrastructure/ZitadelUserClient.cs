using Grpc.Core;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.User.V2;

namespace ProperTea.User.Features.UserProfiles.Infrastructure;

public class ZitadelUserClient : IExternalUserClient
{
    private readonly UserService.UserServiceClient _userClient;
    private readonly ILogger<ZitadelUserClient> _logger;

    public ZitadelUserClient(
        string apiUrl,
        ServiceAccount serviceAccount,
        ILogger<ZitadelUserClient> logger,
        bool allowInsecure = false)
    {
        _userClient = Clients.UserService(
            new(
                apiUrl,
                ITokenProvider.ServiceAccount(
                    apiUrl,
                    serviceAccount,
                    new() { ApiAccess = true, RequireHttps = !allowInsecure })));

        _logger = logger;
    }

    public async Task<ExternalUserDetails?> GetUserDetailsAsync(
        string externalUserId,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserByIDRequest
            {
                UserId = externalUserId
            };

            var response = await _userClient.GetUserByIDAsync(request, cancellationToken: ct);

            // Try to get human user details
            var human = response.User?.Human;
            if (human == null)
            {
                _logger.LogWarning("User {UserId} does not have human profile data", externalUserId);
                return null;
            }

            var email = human.Email?.Email ?? "";
            var firstName = human.Profile?.GivenName;
            var lastName = human.Profile?.FamilyName;

            var userId = response.User?.UserId ?? externalUserId;

            _logger.LogDebug(
                "Retrieved user from ZITADEL: {Email} ({Id})",
                email,
                userId);

            return new ExternalUserDetails(
                userId,
                email,
                firstName,
                lastName);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("User not found in ZITADEL: {Id}", externalUserId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user from ZITADEL: {Id}", externalUserId);
            throw;
        }
    }
}
