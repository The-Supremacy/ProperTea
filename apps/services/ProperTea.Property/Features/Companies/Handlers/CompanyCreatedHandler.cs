using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyCreatedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyCreated message,
        IDocumentStore store)
    {
        await using var session = store.LightweightSession(message.OrganizationId);

        var existing = await session.LoadAsync<CompanyReference>(message.CompanyId);
        if (existing != null && existing.LastUpdatedAt >= message.CreatedAt)
            return;

        session.Store(new CompanyReference
        {
            Id = message.CompanyId,
            Code = message.Code,
            Name = message.Name,
            IsDeleted = false,
            LastUpdatedAt = message.CreatedAt,
            TenantId = message.OrganizationId
        });
        await session.SaveChangesAsync();
    }
}
