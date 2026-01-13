namespace ProperTea.Landlord.Bff.Users;

/// <summary>
/// Client for calling User service backend.
/// Provides typed methods for all user profile operations.
/// </summary>
public class UserClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("user");

    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        return (await _client.GetFromJsonAsync<UserProfileDto>("/users/me", ct))!;
    }
}
