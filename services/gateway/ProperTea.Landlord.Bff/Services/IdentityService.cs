using System.Text;
using System.Text.Json;

namespace ProperTea.Landlord.Bff.Services;

public interface IIdentityService
{
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

    public async Task<string?> ReissueTokenAsync(string expiredToken)
    {
        try
        {
            var requestBody = new { expiredToken };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/reissue", content);

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