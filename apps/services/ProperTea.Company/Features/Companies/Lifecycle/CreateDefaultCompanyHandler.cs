using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public class CreateDefaultCompanyHandler : IWolverineHandler
{
    public async Task Handle(
        IOrganizationRegistered message,
        IDocumentSession session,
        IMessageBus bus)
    {
        var companyId = Guid.NewGuid();
        var created = CompanyAggregate.Create(
            companyId,
            message.Name,
            DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<CompanyAggregate>(companyId, created);

        await session.SaveChangesAsync();

        await bus.PublishAsync(new CompanyIntegrationEvents.CompanyCreated
        {
            CompanyId = companyId,
            OrganizationId = message.OrganizationId,
            Name = message.Name,
            CreatedAt = created.CreatedAt
        });
    }
}
