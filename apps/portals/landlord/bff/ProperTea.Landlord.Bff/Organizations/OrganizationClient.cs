using ProperTea.Landlord.Bff.Errors;

namespace ProperTea.Landlord.Bff.Organizations;

public class OrganizationClientAnonymous(HttpClient httpClient)
{
    public async Task<CheckNameResponse> CheckNameAsync(
        string? name = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(name))
            query.Add($"name={Uri.EscapeDataString(name)}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return (await httpClient.GetFromJsonAsync<CheckNameResponse>(
            $"/organizations_/check-name{queryString}", ct))!;
    }

    public async Task<RegisterOrganizationResponse> RegisterOrganizationAsync(
        RegisterOrganizationRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/organizations", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
        return (await response.Content.ReadFromJsonAsync<RegisterOrganizationResponse>(ct))!;
    }
}

public class OrganizationClient(HttpClient httpClient)
{
    public async Task<OrganizationDetailResponse?> GetOrganizationAsync(string organizationId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<OrganizationDetailResponse>($"/organizations/{organizationId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<AuditLogResponse?> GetAuditLogAsync(string organizationId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<AuditLogResponse>($"/organizations/{organizationId}/audit-log", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
