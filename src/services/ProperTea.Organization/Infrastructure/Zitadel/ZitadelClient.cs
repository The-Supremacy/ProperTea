using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Management.V1;

namespace ProperTea.Organization.Infrastructure.Zitadel;

public interface IZitadelClient
{
    public Task<string> CreateOrganizationAsync(string name, CancellationToken ct = default);
}

public class ZitadelClient : IZitadelClient
{
    private readonly ManagementService.ManagementServiceClient _client;
    private readonly ILogger<ZitadelClient> _logger;

    public ZitadelClient(
        string apiUrl,
        ServiceAccount serviceAccount,
        ILogger<ZitadelClient> logger)
    {
        // Use JWT Profile authentication (same for dev and prod)
        _client = Clients.ManagementService(
            new(
                apiUrl,
                ITokenProvider.ServiceAccount(
                    apiUrl,
                    serviceAccount,
                    new() { ApiAccess = true })));

        _logger = logger;

        _logger.LogInformation(
            "Initialized Zitadel client for service account: {UserId}",
            serviceAccount.UserId);
    }

    public async Task<string> CreateOrganizationAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var request = new AddOrgRequest
            {
                Name = name
            };

            var response = await _client.AddOrgAsync(request, cancellationToken: ct);

            _logger.LogInformation(
                "Created organization in Zitadel: {Name} with ID {OrgId}",
                name,
                response.Id);

            return response.Id;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
        {
            _logger.LogWarning("Organization already exists in Zitadel: {Name}", name);
            throw new InvalidOperationException($"Organization '{name}' already exists in Zitadel", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create organization in Zitadel: {Name}", name);
            throw;
        }
    }
}
