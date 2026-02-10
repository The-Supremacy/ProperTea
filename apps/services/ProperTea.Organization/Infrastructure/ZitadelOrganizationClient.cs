using Grpc.Core;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Org.V2;
using Zitadel.User.V2;
using static Zitadel.Org.V2.AddOrganizationRequest.Types;

namespace ProperTea.Organization.Infrastructure
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
            string password,
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
                                },
                                Password = new Password
                                {
                                    Password_ = password,
                                    ChangeRequired = false
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

        public async Task<bool> CheckOrganizationExistsAsync(
            string orgName,
            CancellationToken ct = default)
        {
            try
            {
                var request = new ListOrganizationsRequest
                {
                    Query = new Zitadel.Object.V2.ListQuery
                    {
                        Limit = 1
                    },
                    Queries =
                    {
                        new Zitadel.Org.V2.SearchQuery
                        {
                            NameQuery = new OrganizationNameQuery
                            {
                                Name = orgName,
                                Method = Zitadel.Object.V2.TextQueryMethod.Equals
                            }
                        }
                    }
                };

                var response = await _orgClient.ListOrganizationsAsync(request, cancellationToken: ct);
                var exists = response.Result.Count > 0;

                _logger.LogDebug(
                    "Checked organization existence in ZITADEL: {Name} = {Exists}",
                    orgName,
                    exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check organization existence in ZITADEL: {Name}", orgName);
                throw;
            }
        }

        public async Task<ExternalOrganizationDetails?> GetOrganizationDetailsAsync(
            string externalOrganizationId,
            CancellationToken ct = default)
        {
            try
            {
                var request = new ListOrganizationsRequest
                {
                    Query = new Zitadel.Object.V2.ListQuery
                    {
                        Limit = 1
                    },
                    Queries =
                    {
                        new Zitadel.Org.V2.SearchQuery
                        {
                            IdQuery = new OrganizationIDQuery
                            {
                                Id = externalOrganizationId
                            }
                        }
                    }
                };

                var response = await _orgClient.ListOrganizationsAsync(request, cancellationToken: ct);

                if (response.Result.Count == 0)
                {
                    _logger.LogWarning("Organization not found in ZITADEL: {Id}", externalOrganizationId);
                    return null;
                }

                var org = response.Result[0];

                _logger.LogDebug(
                    "Retrieved organization from ZITADEL: {Name} ({Id})",
                    org.Name,
                    org.Id);

                return new ExternalOrganizationDetails(
                    org.Name,
                    org.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get organization from ZITADEL: {Id}", externalOrganizationId);
                throw;
            }
        }
    }
}
