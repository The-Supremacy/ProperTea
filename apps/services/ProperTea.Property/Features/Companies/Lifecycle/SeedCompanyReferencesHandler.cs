using Marten;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Lifecycle;

public record CompanySnapshotItem(
    Guid CompanyId,
    string OrganizationId,
    string Code,
    string Name,
    bool IsDeleted,
    DateTimeOffset UpdatedAt);

public record SeedCompanyReferences;

public record SeedCompanyReferencesResult(int Processed, int Skipped);

public class SeedCompanyReferencesHandler : IWolverineHandler
{
    public async Task<SeedCompanyReferencesResult> Handle(
        SeedCompanyReferences command,
        IDocumentStore store,
        IHttpClientFactory httpClientFactory)
    {
        using var client = httpClientFactory.CreateClient("company");
        var snapshot = await client.GetFromJsonAsync<List<CompanySnapshotItem>>(
            "/internal/companies/snapshot")
            ?? [];

        var processed = 0;
        var skipped = 0;

        foreach (var item in snapshot)
        {
            await using var session = store.LightweightSession(item.OrganizationId);

            var existing = await session.LoadAsync<CompanyReference>(item.CompanyId);
            if (existing != null && existing.LastUpdatedAt >= item.UpdatedAt)
            {
                skipped++;
                continue;
            }

            session.Store(new CompanyReference
            {
                Id = item.CompanyId,
                Code = item.Code,
                Name = item.Name,
                IsDeleted = item.IsDeleted,
                LastUpdatedAt = item.UpdatedAt,
                TenantId = item.OrganizationId
            });
            await session.SaveChangesAsync();
            processed++;
        }

        return new SeedCompanyReferencesResult(processed, skipped);
    }
}
