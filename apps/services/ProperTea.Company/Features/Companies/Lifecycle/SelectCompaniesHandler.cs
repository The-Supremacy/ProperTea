using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record SelectCompanies;

public record SelectItem(Guid Id, string Name);

public class SelectCompaniesHandler : IWolverineHandler
{
    public async Task<List<SelectItem>> Handle(
        SelectCompanies command,
        IDocumentSession session)
    {
        var companies = await session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        return [.. companies.Select(c => new SelectItem(c.Id, c.Name))];
    }
}
