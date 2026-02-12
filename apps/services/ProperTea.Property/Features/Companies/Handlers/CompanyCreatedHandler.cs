using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyCreatedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyCreated message,
        IDocumentSession session)
    {
        var reference = new CompanyReference
        {
            Id = message.CompanyId,
            Code = message.Code,
            Name = message.Name,
            IsDeleted = false,
            LastUpdatedAt = message.CreatedAt,
            TenantId = message.OrganizationId.ToString()
        };

        session.Store(reference);
        await session.SaveChangesAsync();
    }
}
