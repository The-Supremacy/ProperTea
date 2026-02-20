using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CompanySnapshotItem(
    Guid CompanyId,
    string OrganizationId,
    string Code,
    string Name,
    bool IsDeleted,
    DateTimeOffset UpdatedAt);

public record GetCompanySnapshot;

public class GetCompanySnapshotHandler : IWolverineHandler
{
    public async Task<List<CompanySnapshotItem>> Handle(
        GetCompanySnapshot query,
        IDocumentStore store)
    {
        await using var session = store.QuerySession();

        var companies = await session.Query<CompanyAggregate>()
            .Where(x => x.AnyTenant())
            .ToListAsync();

        return [.. companies.Select(c => new CompanySnapshotItem(
            c.Id,
            c.TenantId ?? string.Empty,
            c.Code,
            c.Name,
            c.CurrentStatus == CompanyAggregate.Status.Deleted,
            c.UpdatedAt))];
    }
}
