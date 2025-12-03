using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ProperTea.Organization.Utility;

public class KeycloakAdminOptions
{
    public string Authority { get; set; } = string.Empty;
    public string AdminRealm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public interface IKeycloakAdminClient
{
    Task<string> CreateOrganizationAsync(Guid organizationId, string name, string orgAlias);
    Task AddUserToOrganizationAsync(Guid userId, Guid organizationId);
}

public class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakAdminOptions _options;
    private readonly IMemoryCache _cache;
    private const string TokenCacheKey = "keycloak-admin-token";

    public KeycloakAdminClient(HttpClient httpClient,
        IOptions<KeycloakAdminOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<string> CreateOrganizationAsync(Guid organizationId, string name, string orgAlias)
    {
        var accessToken = await GetAccessTokenAsync().ConfigureAwait(false);

        var organizationRepresentation = new
        {
            name = name,
            alias = orgAlias,
            attributes = new Dictionary<string, string[]>
            {
                { "externalId", [organizationId.ToString()] }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.Authority}/admin/realms/{_options.AdminRealm}/organizations")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(organizationRepresentation),
                Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var locationHeader = response.Headers.Location;
        var groupId = locationHeader?.Segments.LastOrDefault();

        return groupId ?? throw new InvalidOperationException("Could not determine new group ID from Keycloak response.");
    }

    public async Task AddUserToOrganizationAsync(Guid userId, Guid organizationId)
    {
        var accessToken = await GetAccessTokenAsync().ConfigureAwait(false);

        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_options.Authority}/admin/realms/{_options.AdminRealm}/organizations/{organizationId}/members")
        {
            Content = new StringContent(
                userId.ToString(),
                Encoding.UTF8)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken))
        {
            return cachedToken!;
        }

        var tokenEndpoint = $"{_options.Authority}/protocol/openid-connect/token";
        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            })
        };

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var tokenResponse = JsonDocument.Parse(content).RootElement;
        var accessToken = tokenResponse.GetProperty("access_token").GetString()!;
        var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();

        _cache.Set(TokenCacheKey, accessToken, TimeSpan.FromSeconds(expiresIn - 30));

        return accessToken;
    }
}
