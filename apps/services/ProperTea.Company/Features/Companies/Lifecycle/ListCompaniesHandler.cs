using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record ListCompanies();

public class ListCompaniesHandler : IWolverineHandler
{
    public async Task<List<CompanyResponse>> Handle(
        ListCompanies query,
        IDocumentSession session)
    {
        var companies = await session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return [.. companies.Select(c => new CompanyResponse(
            c.Id,
            c.Name,
            c.CurrentStatus.ToString(),
            c.CreatedAt))];
    }
}
