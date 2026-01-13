using Grpc.Core;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Management.V1;

namespace ProperTea.Organization.Features.Organizations.Infrastructure;

public interface IZitadelClient
{
    public Task<string> CreateOrganizationAsync(string name, CancellationToken ct = default);
    public Task UpdateOrganizationAsync(string zitadelOrgId, string newName, CancellationToken ct = default);
    public Task<string> GetMyOrganizationIdAsync(CancellationToken ct = default);
    public Task AddUserToOrganizationAsync(string zitadelOrgId, string userId, string[] roles, CancellationToken ct = default);
    public Task AddOrgDomainAsync(string zitadelOrgId, string domain, CancellationToken ct = default);
    public Task VerifyOrgDomainAsync(string zitadelOrgId, string domain, CancellationToken ct = default);
}

public class ZitadelClient : IZitadelClient
{
    private readonly ManagementService.ManagementServiceClient _client;
    private readonly ILogger<ZitadelClient> _logger;

    public ZitadelClient(
        string apiUrl,
        ServiceAccount serviceAccount,
        ILogger<ZitadelClient> logger,
        bool allowInsecure = false)
    {
        // Use JWT Profile authentication (same for dev and prod)
        _client = Clients.ManagementService(
            new(
                apiUrl,
                ITokenProvider.ServiceAccount(
                    apiUrl,
                    serviceAccount,
                    new() { ApiAccess = true, RequireHttps = !allowInsecure })));

        _logger = logger;

        _logger.LogInformation(
            "Initialized Zitadel client for service account: {UserId} (allowInsecure: {AllowInsecure})",
            serviceAccount.UserId,
            allowInsecure);
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
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
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

    public async Task UpdateOrganizationAsync(string zitadelOrgId, string newName, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateOrgRequest
            {
                Name = newName
            };

            // Set organization context via metadata header
            var headers = new Metadata
            {
                { "x-zitadel-orgid", zitadelOrgId }
            };

            _ = await _client.UpdateOrgAsync(request, headers, cancellationToken: ct);

            _logger.LogInformation(
                "Updated organization in Zitadel: {ZitadelOrgId} with new name {NewName}",
                zitadelOrgId,
                newName);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            _logger.LogWarning(
                "Organization name already exists in Zitadel: {NewName}",
                newName);
            throw new InvalidOperationException($"Organization name '{newName}' already exists in Zitadel", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to update organization in Zitadel: {ZitadelOrgId}",
                zitadelOrgId);
            throw;
        }
    }

    public async Task<string> GetMyOrganizationIdAsync(CancellationToken ct = default)
    {
        try
        {
            var request = new GetMyOrgRequest();
            var response = await _client.GetMyOrgAsync(request, cancellationToken: ct);

            _logger.LogInformation(
                "Retrieved current organization: {OrgId}",
                response.Org.Id);

            return response.Org.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current organization");
            throw;
        }
    }

    public async Task AddUserToOrganizationAsync(string zitadelOrgId, string userId, string[] roles, CancellationToken ct = default)
    {
        try
        {
            var request = new AddOrgMemberRequest
            {
                UserId = userId,
                Roles = { roles }
            };

            // Set organization context via metadata header
            var headers = new Metadata
            {
                { "x-zitadel-orgid", zitadelOrgId }
            };

            _ = await _client.AddOrgMemberAsync(request, headers, cancellationToken: ct);

            _logger.LogInformation(
                "Added user {UserId} to organization {OrgId} with roles: {Roles}",
                userId,
                zitadelOrgId,
                string.Join(", ", roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user {UserId} to organization {OrgId}", userId, zitadelOrgId);
            throw;
        }
    }

    public async Task AddOrgDomainAsync(string zitadelOrgId, string domain, CancellationToken ct = default)
    {
        try
        {
            var request = new AddOrgDomainRequest
            {
                Domain = domain
            };

            var headers = new Metadata
            {
                { "x-zitadel-orgid", zitadelOrgId }
            };

            _ = await _client.AddOrgDomainAsync(request, headers, cancellationToken: ct);

            _logger.LogInformation(
                "Added domain {Domain} to organization {OrgId}",
                domain,
                zitadelOrgId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add domain {Domain} to organization {OrgId}", domain, zitadelOrgId);
            throw;
        }
    }

    public async Task VerifyOrgDomainAsync(string zitadelOrgId, string domain, CancellationToken ct = default)
    {
        try
        {
            var request = new ValidateOrgDomainRequest
            {
                Domain = domain
            };

            var headers = new Metadata
            {
                { "x-zitadel-orgid", zitadelOrgId }
            };

            _ = await _client.ValidateOrgDomainAsync(request, headers, cancellationToken: ct);

            _logger.LogInformation(
                "Verified domain {Domain} for organization {OrgId}",
                domain,
                zitadelOrgId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify domain {Domain} for organization {OrgId}", domain, zitadelOrgId);
            throw;
        }
    }
}
