using Grpc.Core;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Org.V2;
using Zitadel.User.V2;
using static Zitadel.Org.V2.AddOrganizationRequest.Types;

namespace ProperTea.Organization.Features.Organizations.Infrastructure
{
    public class ZitadelOrganizationClient : IExternalOrganizationClient
    {
        private readonly OrganizationService.OrganizationServiceClient _orgClient;
        private readonly ILogger<ZitadelOrganizationClient> _logger;

        public ZitadelOrganizationClient(
            string apiUrl,
            ServiceAccount serviceAccount,
            ILogger<ZitadelOrganizationClient> logger,
            bool allowInsecure = false)
        {
            _orgClient = Clients.OrganizationService(
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

        public async Task<string> CreateOrganizationWithAdminAsync(
            string orgName,
            string email,
            string firstName,
            string lastName,
            CancellationToken ct = default)
        {
            try
            {
                var request = new AddOrganizationRequest
                {
                    Name = orgName,
                    Admins =
                    {
                        new Admin
                        {
                            Human = new AddHumanUserRequest()
                            {
                                Email = new SetHumanEmail { Email = email },
                                Profile = new SetHumanProfile
                                {
                                    GivenName = firstName,
                                    FamilyName = lastName
                                }
                            }
                        }
                    }
                };

                var response = await _orgClient.AddOrganizationAsync(request, cancellationToken: ct);

                _logger.LogInformation(
                    "Created organization in Zitadel: {Name} with ID {OrgId}",
                    orgName,
                    response.OrganizationId);

                return response.OrganizationId;
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
    }
}
