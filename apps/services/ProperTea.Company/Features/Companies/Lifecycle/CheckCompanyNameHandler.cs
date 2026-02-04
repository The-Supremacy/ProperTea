using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CheckCompanyName(string Name, Guid? ExcludeId = null);

public record CheckCompanyNameResult(bool Available, Guid? ExistingCompanyId);

public class CheckCompanyNameHandler : IWolverineHandler
{
    public async Task<CheckCompanyNameResult> Handle(
        CheckCompanyName query,
        IDocumentSession session)
    {
        // Find active company with same name (case-insensitive)
        var existingCompany = await session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active)
            .Where(c => c.Name.Equals(query.Name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefaultAsync();

        // If no company found, name is available
        if (existingCompany == null)
            return new CheckCompanyNameResult(true, null);

        // If found company is the one we're excluding (updating current company), name is available
        if (query.ExcludeId.HasValue && existingCompany.Id == query.ExcludeId.Value)
            return new CheckCompanyNameResult(true, null);

        // Name is taken by another company
        return new CheckCompanyNameResult(false, existingCompany.Id);
    }
}
