using System.Text;
using System.Text.Json;
using ProperTea.Landlord.Bff.DTOs;
using ProperTea.Landlord.Bff.DTOs.Auth;

namespace ProperTea.Landlord.Bff.Services;

public interface IIdentityService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<string?> ReissueTokenAsync(string expiredToken);
}

public class IdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(HttpClient httpClient, ILogger<IdentityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/token/login", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseElement = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = responseElement.GetProperty("accessToken").GetString();
            var userId = responseElement.GetProperty("user").GetProperty("id").GetString();

            if (accessToken is null || userId is null)
            {
                _logger.LogError("Login response from identity service is missing required properties.");
                return null;
            }

            return new LoginResponse(accessToken, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login.");
            throw;
        }
    }
    
    public async Task<string?> ReissueTokenAsync(string expiredToken)
    {
        try
        {
            var requestBody = new { expiredToken };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/token/reissue", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to reissue token. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            return responseContent.GetProperty("accessToken").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reissuing the token.");
            return null;
        }
    }
}