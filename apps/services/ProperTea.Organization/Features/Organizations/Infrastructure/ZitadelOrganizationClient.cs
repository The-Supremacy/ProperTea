using Grpc.Core;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Management.V1;

namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public class ZitadelOrganizationClient : IExternalOrganizationClient
    {
        private readonly ManagementService.ManagementServiceClient _client;
        private readonly ILogger<ZitadelOrganizationClient> _logger;

        public ZitadelOrganizationClient(
            string apiUrl,
            ServiceAccount serviceAccount,
            ILogger<ZitadelOrganizationClient> logger,
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

        public async Task<string> CreateOrganizationAsync(string orgName, CancellationToken ct = default)
        {
            try
            {
                var request = new AddOrgRequest
                {
                    Name = orgName
                };

                var response = await _client.AddOrgAsync(request, cancellationToken: ct);

                _logger.LogInformation(
                    "Created organization in Zitadel: {Name} with ID {OrgId}",
                    orgName,
                    response.Id);

                return response.Id;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
            {
                _logger.LogWarning("Organization already exists in Zitadel: {Name}", orgName);
                throw new InvalidOperationException($"Organization '{orgName}' already exists in Zitadel", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create organization in Zitadel: {Name}", orgName);
                throw;
            }
        }

        public async Task UpdateOrganizationAsync(string externalOrgId, string newName, CancellationToken ct = default)
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
                    { "x-zitadel-orgid", externalOrgId }
                };

                _ = await _client.UpdateOrgAsync(request, headers, cancellationToken: ct);

                _logger.LogInformation(
                    "Updated organization in Zitadel: {ZitadelOrgId} with new name {NewName}",
                    externalOrgId,
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
                    externalOrgId);
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

        public async Task AddUserToOrganizationAsync(string externalOrgId, string userId, string[] roles, CancellationToken ct = default)
        {
            try
            {
                var request = new AddOrgMemberRequest
                {
                    UserId = userId,
                    Roles = { roles }
                };

                var headers = new Metadata
                {
                    { "x-zitadel-orgid", externalOrgId }
                };

                _ = await _client.AddOrgMemberAsync(request, headers, cancellationToken: ct);

                _logger.LogInformation(
                    "Added user {UserId} to organization {OrgId} with roles: {Roles}",
                    userId,
                    externalOrgId,
                    string.Join(", ", roles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user {UserId} to organization {OrgId}", userId, externalOrgId);
                throw;
            }
        }


        public async Task RemoveUserFromOrganizationAsync(string externalOrgId, string userId, CancellationToken ct = default)
        {
            try
            {
                var request = new RemoveOrgMemberRequest
                {
                    UserId = userId
                };

                var headers = new Metadata
                {
                    { "x-zitadel-orgid", externalOrgId }
                };

                _ = await _client.RemoveOrgMemberAsync(request, headers, cancellationToken: ct);

                _logger.LogInformation(
                    "Removed user {UserId} from organization {OrgId}",
                    userId,
                    externalOrgId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user {UserId} from organization {OrgId}", userId, externalOrgId);
                throw;
            }
        }

        public async Task AddOrgDomainAsync(string externalOrgId, string domain, CancellationToken ct = default)
        {
            try
            {
                var request = new AddOrgDomainRequest
                {
                    Domain = domain
                };

                var headers = new Metadata
                {
                    { "x-zitadel-orgid", externalOrgId }
                };

                _ = await _client.AddOrgDomainAsync(request, headers, cancellationToken: ct);

                _logger.LogInformation(
                    "Added domain {Domain} to organization {OrgId}",
                    domain,
                    externalOrgId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add domain {Domain} to organization {OrgId}", domain, externalOrgId);
                throw;
            }
        }

        public async Task VerifyOrgDomainAsync(string externalOrgId, string domain, CancellationToken ct = default)
        {
            try
            {
                var request = new ValidateOrgDomainRequest
                {
                    Domain = domain
                };

                var headers = new Metadata
                {
                    { "x-zitadel-orgid", externalOrgId }
                };

                _ = await _client.ValidateOrgDomainAsync(request, headers, cancellationToken: ct);

                _logger.LogInformation(
                    "Verified domain {Domain} for organization {OrgId}",
                    domain,
                    externalOrgId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify domain {Domain} for organization {OrgId}", domain, externalOrgId);
                throw;
            }
        }
    }
}
