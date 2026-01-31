using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record UpdateCompanyName(Guid CompanyId, string Name);

public class UpdateCompanyNameHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateCompanyName command,
        IDocumentSession session)
    {
        var company = await session.Events.AggregateStreamAsync<CompanyAggregate>(command.CompanyId)
            ?? throw new NotFoundException(
                CompanyErrorCodes.COMPANY_NOT_FOUND,
                "Company",
                command.CompanyId);

        var updated = company.UpdateName(command.Name);
        _ = session.Events.Append(command.CompanyId, updated);
    }
}
