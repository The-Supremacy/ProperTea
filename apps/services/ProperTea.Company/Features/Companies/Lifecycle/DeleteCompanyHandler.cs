using Marten;
using ProperTea.Infrastructure.Common.Exceptions;
using Wolverine;

namespace ProperTea.Company.Features.Companies.Lifecycle;

public record DeleteCompany(Guid CompanyId);

public class DeleteCompanyHandler : IWolverineHandler
{
    public async Task Handle(
        DeleteCompany command,
        IDocumentSession session,
        IMessageBus bus)
    {
        var company = await session.Events.AggregateStreamAsync<CompanyAggregate>(command.CompanyId)
            ?? throw new NotFoundException(
                CompanyErrorCodes.COMPANY_NOT_FOUND,
                "Company",
                command.CompanyId);

        var deleted = company.Delete(DateTimeOffset.UtcNow);
        _ = session.Events.Append(command.CompanyId, deleted);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new CompanyIntegrationEvents.CompanyDeleted
        {
            CompanyId = command.CompanyId,
            OrganizationId = Guid.Parse(session.TenantId),
            DeletedAt = deleted.DeletedAt
        });
    }
}
