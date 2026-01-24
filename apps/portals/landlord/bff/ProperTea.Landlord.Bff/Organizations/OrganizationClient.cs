namespace ProperTea.Landlord.Bff.Organizations;

public class OrganizationClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("organization");

    public async Task<OrganizationDto?> GetOrganizationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<OrganizationDto>($"/organizations/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<RegisterOrganizationResponse> RegisterOrganizationAsync(
        RegisterOrganizationRequest request,
        CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/organizations", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterOrganizationResponse>(ct))!;
    }

    public async Task<CheckAvailabilityResponse> CheckAvailabilityAsync(
        string? name = null,
        string? slug = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(name))
            query.Add($"name={Uri.EscapeDataString(name)}");
        if (!string.IsNullOrWhiteSpace(slug))
            query.Add($"slug={Uri.EscapeDataString(slug)}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return (await _client.GetFromJsonAsync<CheckAvailabilityResponse>(
            $"/organizations/check-availability{queryString}", ct))!;
    }

    public async Task<AuditLogResponse?> GetAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<AuditLogResponse>($"/organizations/{id}/audit-log", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
