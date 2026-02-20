using ProperTea.Infrastructure.Common.Pagination;
using ProperTea.Landlord.Bff.Errors;

namespace ProperTea.Landlord.Bff.Buildings;

public record BuildingAddressDto(string Country, string City, string ZipCode, string StreetAddress);

public record EntranceItemDto(Guid Id, string Code, string Name);

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
    BuildingAddressDto? Address,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<EntranceItemDto> Entrances);

public record BuildingSelectItem(Guid Id, string Code, string Name);

public record CreateBuildingRequest(string Code, string Name, BuildingAddressDto? Address);
public record UpdateBuildingRequest(string? Code, string? Name, BuildingAddressDto? Address);
public record AddEntranceRequest(string Code, string Name);

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

        await response.EnsureSuccessOrProxyAsync(ct);
        return await response.Content.ReadFromJsonAsync<BuildingDetailResponse>(ct);
    }

    public async Task<object> CreateBuildingAsync(Guid propertyId, CreateBuildingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/properties/{propertyId}/buildings", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
        return await response.Content.ReadFromJsonAsync<object>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize building response");
    }

    public async Task UpdateBuildingAsync(Guid id, UpdateBuildingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/buildings/{id}", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
    }

    public async Task DeleteBuildingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/buildings/{id}", ct);
        await response.EnsureSuccessOrProxyAsync(ct);
    }

    public async Task<BuildingAuditLogResponse> GetBuildingAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<BuildingAuditLogResponse>($"/buildings/{id}/audit-log", ct)
            ?? throw new InvalidOperationException("Failed to fetch building audit log");
    }

    public async Task<Guid> AddEntranceAsync(Guid buildingId, AddEntranceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/buildings/{buildingId}/entrances", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        return result.GetProperty("id").GetGuid();
    }

    public async Task RemoveEntranceAsync(Guid buildingId, Guid entranceId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/buildings/{buildingId}/entrances/{entranceId}", ct);
        await response.EnsureSuccessOrProxyAsync(ct);
    }
}
