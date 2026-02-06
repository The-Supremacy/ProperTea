using ProperTea.Infrastructure.Common.ErrorHandling;

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
            $"/organizations/check-name{queryString}", ct))!;
    }

    public async Task<RegisterOrganizationResponse> RegisterOrganizationAsync(
        RegisterOrganizationRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/organizations", request, ct);
        await response.EnsureDownstreamSuccessAsync(ct: ct);
        return (await response.Content.ReadFromJsonAsync<RegisterOrganizationResponse>(ct))!;
    }
}

public class OrganizationClient(HttpClient httpClient)
{
    public async Task<OrganizationDto?> GetOrganizationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<OrganizationDto>($"/organizations/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<AuditLogResponse?> GetAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<AuditLogResponse>($"/organizations/{id}/audit-log", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
