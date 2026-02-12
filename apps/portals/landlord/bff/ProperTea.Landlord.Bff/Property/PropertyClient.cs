using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Property;

public record PropertyListItem(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage,
    int BuildingCount,
    string Status,
    DateTimeOffset CreatedAt);

public record PagedPropertiesResponse(
    IReadOnlyList<PropertyListItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record PropertyDetailResponse(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage,
    IReadOnlyList<BuildingResponse> Buildings,
    string Status,
    DateTimeOffset CreatedAt);

public record BuildingResponse(Guid Id, string Code, string Name);

public record PropertySelectItem(Guid Id, string Name);

public record CreatePropertyRequest(
    Guid CompanyId,
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public record UpdatePropertyRequest(
    string Code,
    string Name,
    string Address,
    decimal? SquareFootage);

public class PropertyClient(HttpClient httpClient)
{
    public async Task<PagedPropertiesResponse> GetPropertiesAsync(
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

        return await httpClient.GetFromJsonAsync<PagedPropertiesResponse>($"/properties?{queryString}", ct)
            ?? new PagedPropertiesResponse([], 0, pagination.Page, pagination.PageSize);
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
