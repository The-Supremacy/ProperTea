using ProperTea.Infrastructure.Common.Pagination;
using ProperTea.Landlord.Bff.Errors;

namespace ProperTea.Landlord.Bff.Units;

public record AddressResponse(string Country, string City, string ZipCode, string StreetAddress);

public record UnitListItem(
    Guid Id,
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    string UnitReference,
    string Category,
    AddressResponse Address,
    int? Floor,
    string Status,
    DateTimeOffset CreatedAt);

public record UnitDetailResponse(
    Guid Id,
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    string UnitReference,
    string Category,
    AddressResponse Address,
    int? Floor,
    string Status,
    DateTimeOffset CreatedAt);

public record UnitSelectItem(Guid Id, string Code, string UnitReference);

public record AddressRequest(string Country, string City, string ZipCode, string StreetAddress);

public record CreateUnitRequest(
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    string Category,
    AddressRequest Address,
    int? Floor);

public record UpdateUnitRequest(
    Guid PropertyId,
    Guid? BuildingId,
    Guid? EntranceId,
    string Code,
    string Category,
    AddressRequest Address,
    int? Floor);

public record UnitAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public record UnitAuditLogResponse(
    Guid UnitId,
    IReadOnlyList<UnitAuditLogEntry> Entries);

public class UnitClient(HttpClient httpClient)
{
    public async Task<PagedResult<UnitListItem>> GetUnitsAsync(
        Guid? propertyId,
        ListUnitsQuery query,
        PaginationQuery pagination,
        SortQuery sort,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

        queryString["page"] = pagination.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        queryString["pageSize"] = pagination.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (propertyId.HasValue)
            queryString["propertyId"] = propertyId.Value.ToString();

        if (query.BuildingId.HasValue)
            queryString["buildingId"] = query.BuildingId.Value.ToString();

        if (!string.IsNullOrWhiteSpace(query.Code))
            queryString["code"] = query.Code;

        if (!string.IsNullOrWhiteSpace(query.UnitReference))
            queryString["unitReference"] = query.UnitReference;

        if (!string.IsNullOrWhiteSpace(query.Category))
            queryString["category"] = query.Category;

        if (query.Floor.HasValue)
            queryString["floor"] = query.Floor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(sort.Sort))
            queryString["sort"] = sort.Sort;

        return await httpClient.GetFromJsonAsync<PagedResult<UnitListItem>>($"/units?{queryString}", ct)
            ?? new PagedResult<UnitListItem> { Items = [], TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize };
    }

    public async Task<List<UnitSelectItem>> SelectUnitsAsync(Guid propertyId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<UnitSelectItem>>($"/properties/{propertyId}/units/select", ct)
            ?? [];
    }

    public async Task<UnitDetailResponse?> GetUnitAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/units/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await response.EnsureSuccessOrProxyAsync(ct);
        return await response.Content.ReadFromJsonAsync<UnitDetailResponse>(ct);
    }

    public async Task<object> CreateUnitAsync(CreateUnitRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/units", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
        return await response.Content.ReadFromJsonAsync<object>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize unit response");
    }

    public async Task UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/units/{id}", request, ct);
        await response.EnsureSuccessOrProxyAsync(ct);
    }

    public async Task DeleteUnitAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/units/{id}", ct);
        await response.EnsureSuccessOrProxyAsync(ct);
    }

    public async Task<UnitAuditLogResponse> GetUnitAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<UnitAuditLogResponse>($"/units/{id}/audit-log", ct)
            ?? new UnitAuditLogResponse(id, []);
    }
}
