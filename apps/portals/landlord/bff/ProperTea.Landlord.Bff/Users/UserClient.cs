using ProperTea.Infrastructure.Common.ErrorHandling;

namespace ProperTea.Landlord.Bff.Users;

public class UserClient(HttpClient httpClient)
{
    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        return (await httpClient.GetFromJsonAsync<UserProfileDto>("/users/me", ct))!;
    }

    public async Task<UserPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<UserPreferencesDto?>("/users/preferences", ct);
    }

    public async Task UpdatePreferencesAsync(UpdateUserPreferencesRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/users/preferences", request, ct);
        _ = response.EnsureDownstreamSuccessAsync(ct: ct);
    }
}
