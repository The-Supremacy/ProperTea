namespace ProperTea.Landlord.Bff.Companies;

public class CompanyClient(HttpClient httpClient)
{
    public async Task<CompanyResponse> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/companies", request, ct);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyResponse>(ct)
            ?? throw new InvalidOperationException("Failed to deserialize company response");
    }

    public async Task<List<CompanyListItem>> GetCompaniesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<CompanyListItem>>("/companies", ct)
            ?? [];
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
}

// DTOs
public record CreateCompanyRequest(string Name);

public record UpdateCompanyNameRequest(string Name);

public record CompanyResponse(Guid Id);

public record CompanyListItem(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);

public record CompanyDetailResponse(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);
