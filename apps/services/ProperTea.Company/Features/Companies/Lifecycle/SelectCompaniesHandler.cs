using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record SelectCompanies;

public record SelectItem(Guid Id, string Code, string Name);

public class SelectCompaniesHandler : IWolverineHandler
{
    public async Task<List<SelectItem>> Handle(
        SelectCompanies command,
        IDocumentSession session)
    {
        var companies = await session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active)
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync();

        return [.. companies.Select(c => new SelectItem(c.Id, c.Code, c.Name))];
    }
}
