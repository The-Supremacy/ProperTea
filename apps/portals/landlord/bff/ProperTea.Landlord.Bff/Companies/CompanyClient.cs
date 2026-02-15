using ProperTea.Infrastructure.Common.Pagination;

namespace ProperTea.Landlord.Bff.Companies;

public record CreateCompanyRequest(string Code, string Name);

public record UpdateCompanyRequest(string? Code, string? Name);

public record CompanyAuditLogResponse(
    Guid CompanyId,
    IReadOnlyList<CompanyAuditLogEntry> Entries);

public record CompanyAuditLogEntry(
    string EventType,
    DateTimeOffset Timestamp,
    string? Username,
    int Version,
    object Data);

public record CompanyResponse(Guid Id);

public record CompanyListItem(Guid Id, string Code, string Name, string Status, DateTimeOffset CreatedAt);

public record CompanyDetailResponse(Guid Id, string Code, string Name, string Status, DateTimeOffset CreatedAt);



public record CheckNameResponse(bool Available, Guid? ExistingCompanyId);

public record CheckCodeResponse(bool Available, Guid? ExistingCompanyId);

public record CompanySelectItem(Guid Id, string Code, string Name);

public class CompanyClient(HttpClient httpClient)
{
    public async Task<CompanyResponse> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/companies", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyResponse>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize company response");
    }

    public async Task<PagedResult<CompanyListItem>> GetCompaniesAsync(
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

        return await httpClient.GetFromJsonAsync<PagedResult<CompanyListItem>>($"/companies?{queryString}", ct)
            ?? new PagedResult<CompanyListItem> { Items = [], TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize };
    }

    public async Task<CompanyDetailResponse?> GetCompanyAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/companies/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyDetailResponse>(ct);
    }

    public async Task UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/companies/{id}", request, ct);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<CompanyAuditLogResponse> GetCompanyAuditLogAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<CompanyAuditLogResponse>($"/companies/{id}/audit-log", ct)
            ?? throw new InvalidOperationException("Failed to fetch company audit log");
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

        return await httpClient.GetFromJsonAsync<CheckNameResponse>($"/companies_/check-name?{queryString}", ct)
            ?? throw new InvalidOperationException("Failed to check company name");
    }

    public async Task<CheckCodeResponse> CheckCompanyCodeAsync(
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString["code"] = code;

        if (excludeId.HasValue)
            queryString["excludeId"] = excludeId.Value.ToString();

        return await httpClient.GetFromJsonAsync<CheckCodeResponse>($"/companies_/check-code?{queryString}", ct)
            ?? throw new InvalidOperationException("Failed to check company code");
    }

    public async Task<List<CompanySelectItem>> SelectCompaniesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<CompanySelectItem>>("/companies/select", ct)
            ?? [];
    }
}
