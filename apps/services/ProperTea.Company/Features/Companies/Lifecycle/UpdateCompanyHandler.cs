using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record UpdateCompany(Guid CompanyId, string Name);

public class UpdateCompanyHandler : IWolverineHandler
{
    public async Task Handle(
        UpdateCompany command,
        IDocumentSession session)
    {
        var company = await session.Events.AggregateStreamAsync<CompanyAggregate>(command.CompanyId)
            ?? throw new NotFoundException(
                CompanyErrorCodes.COMPANY_NOT_FOUND,
                "Company",
                command.CompanyId);

        // Compare fields and emit events only for changes
        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(command.Name) && company.Name != command.Name)
        {
            events.Add(company.UpdateName(command.Name));
        }

        // Append all change events
        if (events.Count > 0)
        {
            _ = session.Events.Append(command.CompanyId, [.. events]);
        }
    }
}
