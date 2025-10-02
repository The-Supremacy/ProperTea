using System.Text.Json;

namespace ProperTea.ApiGateway.Services;

public interface IAuthorizationService
{
    Task<PermissionsModel> GetPermissionsAsync(string userId, string organizationId);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(HttpClient httpClient, ILogger<AuthorizationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PermissionsModel> GetPermissionsAsync(string userId, string organizationId)
    {
        var response = await _httpClient.GetAsync($"/auth/user/{userId}/org/{organizationId}/permissions-model");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PermissionsModel>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        return result ?? throw new InvalidOperationException("Failed to deserialize permissions model");
    }
}