using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record GetCompany(Guid CompanyId);

public record CompanyResponse(Guid Id, string Code, string Name, string Status, DateTimeOffset CreatedAt);

public class GetCompanyHandler : IWolverineHandler
{
    public async Task<CompanyResponse?> Handle(
        GetCompany query,
        IDocumentSession session)
    {
        var company = await session.Events.AggregateStreamAsync<CompanyAggregate>(query.CompanyId);

        if (company == null)
            return null;

        return new CompanyResponse(
            company.Id,
            company.Code,
            company.Name,
            company.CurrentStatus.ToString(),
            company.CreatedAt);
    }
}
