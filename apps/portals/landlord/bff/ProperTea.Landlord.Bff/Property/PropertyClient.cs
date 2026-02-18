using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Property;

public record PropertyListItem(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string Code,
    string Name,
    string Address,
    int BuildingCount,
    string Status,
    DateTimeOffset CreatedAt);



public record PropertyDetailResponse(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    string Status,
    DateTimeOffset CreatedAt);

public record PropertySelectItem(Guid Id, string Name);

public record CreatePropertyRequest(
    Guid CompanyId,
    string Code,
    string Name,
    string Address);

public record UpdatePropertyRequest(
    string? Code,
    string? Name,
    string? Address);

public record PropertyAuditLogResponse(
    Guid PropertyId,
    IReadOnlyList<PropertyAuditLogEntry> Entries);

public record PropertyAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public class PropertyClient(HttpClient httpClient)
{
    public async Task<PagedResult<PropertyListItem>> GetPropertiesAsync(
        ListPropertiesQuery query,
        PaginationQuery pagination,
        SortQuery sort,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

        queryString["page"] = pagination.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        queryString["pageSize"] = pagination.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryString["name"] = query.Name;

        if (!string.IsNullOrWhiteSpace(query.Code))
            queryString["code"] = query.Code;

        if (query.CompanyId.HasValue)
            queryString["companyId"] = query.CompanyId.Value.ToString();

        if (!string.IsNullOrWhiteSpace(sort.Sort))
            queryString["sort"] = sort.Sort;

        return await httpClient.GetFromJsonAsync<PagedResult<PropertyListItem>>($"/properties?{queryString}", ct)
            ?? new PagedResult<PropertyListItem> { Items = [], TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize };
    }

    public async Task<PropertyDetailResponse?> GetPropertyAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/properties/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PropertyDetailResponse>(ct);
    }

    public async Task<object> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/properties", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<object>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize property response");
    }

    public async Task UpdatePropertyAsync(Guid id, UpdatePropertyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/properties/{id}", request, ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task DeletePropertyAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/properties/{id}", ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<PropertyAuditLogResponse> GetPropertyAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PropertyAuditLogResponse>($"/properties/{id}/audit-log", ct)
            ?? throw new InvalidOperationException("Failed to fetch property audit log");
    }

    public async Task<List<PropertySelectItem>> SelectPropertiesAsync(
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

        if (companyId.HasValue)
            queryString["companyId"] = companyId.Value.ToString();

        var qs = queryString.ToString();
        var url = string.IsNullOrEmpty(qs) ? "/properties/select" : $"/properties/select?{qs}";

        return await httpClient.GetFromJsonAsync<List<PropertySelectItem>>(url, ct)
            ?? [];
    }
}
