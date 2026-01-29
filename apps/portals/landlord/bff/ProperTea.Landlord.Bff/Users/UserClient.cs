namespace ProperTea.Landlord.Bff.Users;

public class UserClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("user");

    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        return (await _client.GetFromJsonAsync<UserProfileDto>("/users/me", ct))!;
    }
}
