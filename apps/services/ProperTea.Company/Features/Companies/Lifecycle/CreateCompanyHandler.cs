using Marten;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record CreateCompany(string Name);

public class CreateCompanyHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateCompany command,
        IDocumentSession session)
    {
        var companyId = Guid.NewGuid();
        var created = CompanyAggregate.Create(companyId, command.Name, DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<CompanyAggregate>(companyId, created);

        // Explicit save to ensure ID is available immediately for response
        await session.SaveChangesAsync();

        return companyId;
    }
}
