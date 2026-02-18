using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CheckCompanyCode(string Code, Guid? ExcludeId = null);

public record CheckCompanyCodeResult(bool Available, Guid? ExistingCompanyId);

public class CheckCompanyCodeHandler : IWolverineHandler
{
    public async Task<CheckCompanyCodeResult> Handle(
        CheckCompanyCode query,
        IDocumentSession session)
    {
        var existingCompany = await session.Query<CompanyAggregate>()
            .Where(c => c.CurrentStatus == CompanyAggregate.Status.Active)
            .Where(c => c.Code.Equals(query.Code, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefaultAsync();

        if (existingCompany == null)
            return new CheckCompanyCodeResult(true, null);

        if (query.ExcludeId.HasValue && existingCompany.Id == query.ExcludeId.Value)
            return new CheckCompanyCodeResult(true, null);

        return new CheckCompanyCodeResult(false, existingCompany.Id);
    }
}
