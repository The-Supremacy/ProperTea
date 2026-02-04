using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Companies;

public record CreateCompanyRequest(string Name);

public record UpdateCompanyNameRequest(string Name);

public record CompanyResponse(Guid Id);

public record CompanyListItem(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);

public record CompanyDetailResponse(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);

public record PagedCompaniesResponse(
    IReadOnlyList<CompanyListItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record CheckNameResponse(bool Available, Guid? ExistingCompanyId);

public class CompanyClient(HttpClient httpClient)
{
    public async Task<CompanyResponse> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/companies", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyResponse>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize company response");
    }

    public async Task<PagedCompaniesResponse> GetCompaniesAsync(
        ListCompaniesQuery query,
        PaginationQuery pagination,
        SortQuery sort,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

        queryString["page"] = pagination.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        queryString["pageSize"] = pagination.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryString["name"] = query.Name;

        if (!string.IsNullOrWhiteSpace(sort.Sort))
            queryString["sort"] = sort.Sort;

        return await httpClient.GetFromJsonAsync<PagedCompaniesResponse>($"/companies?{queryString}", ct)
            ?? new PagedCompaniesResponse([], 0, pagination.Page, pagination.PageSize);
    }

    public async Task<CompanyDetailResponse?> GetCompanyAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/companies/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyDetailResponse>(ct);
    }

    public async Task UpdateCompanyNameAsync(Guid id, UpdateCompanyNameRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/companies/{id}", request, ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCompanyAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/companies/{id}", ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<CheckNameResponse> CheckCompanyNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString["name"] = name;

        if (excludeId.HasValue)
            queryString["excludeId"] = excludeId.Value.ToString();

        return await httpClient.GetFromJsonAsync<CheckNameResponse>($"/companies/check-name?{queryString}", ct)
            ?? throw new InvalidOperationException("Failed to check company name");
    }
}
