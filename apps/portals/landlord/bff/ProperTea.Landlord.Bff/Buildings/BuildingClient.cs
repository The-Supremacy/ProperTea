using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Buildings;

public record BuildingListItem(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);



public record BuildingDetailResponse(
    Guid Id,
    Guid PropertyId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public record BuildingSelectItem(Guid Id, string Code, string Name);

public record CreateBuildingRequest(string Code, string Name);

public record UpdateBuildingRequest(string? Code, string? Name);

public record BuildingAuditLogResponse(
    Guid BuildingId,
    IReadOnlyList<BuildingAuditLogEntry> Entries);

public record BuildingAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public class BuildingClient(HttpClient httpClient)
{
    public async Task<PagedResult<BuildingListItem>> GetBuildingsAsync(
        Guid? propertyId,
        ListBuildingsQuery query,
        PaginationQuery pagination,
        SortQuery sort,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

        queryString["page"] = pagination.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        queryString["pageSize"] = pagination.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (propertyId.HasValue)
            queryString["propertyId"] = propertyId.Value.ToString();

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryString["name"] = query.Name;

        if (!string.IsNullOrWhiteSpace(query.Code))
            queryString["code"] = query.Code;

        if (!string.IsNullOrWhiteSpace(sort.Sort))
            queryString["sort"] = sort.Sort;

        return await httpClient.GetFromJsonAsync<PagedResult<BuildingListItem>>($"/buildings?{queryString}", ct)
            ?? new PagedResult<BuildingListItem> { Items = [], TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize };
    }

    public async Task<List<BuildingSelectItem>> SelectBuildingsAsync(Guid propertyId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<BuildingSelectItem>>($"/properties/{propertyId}/buildings/select", ct)
            ?? [];
    }

    public async Task<BuildingDetailResponse?> GetBuildingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/buildings/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BuildingDetailResponse>(ct);
    }

    public async Task<object> CreateBuildingAsync(Guid propertyId, CreateBuildingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/properties/{propertyId}/buildings", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<object>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize building response");
    }

    public async Task UpdateBuildingAsync(Guid id, UpdateBuildingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/buildings/{id}", request, ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task DeleteBuildingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/buildings/{id}", ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<BuildingAuditLogResponse> GetBuildingAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<BuildingAuditLogResponse>($"/buildings/{id}/audit-log", ct)
            ?? throw new InvalidOperationException("Failed to fetch building audit log");
    }
}
