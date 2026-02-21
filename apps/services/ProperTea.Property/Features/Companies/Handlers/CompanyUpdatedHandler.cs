using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyUpdatedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyUpdated message,
        IDocumentStore store)
    {
        await using var session = store.LightweightSession(message.OrganizationId);

        var existing = await session.LoadAsync<CompanyReference>(message.CompanyId);
        if (existing != null && existing.LastUpdatedAt >= message.UpdatedAt)
            return;

        session.Store(new CompanyReference
        {
            Id = message.CompanyId,
            Code = message.Code,
            Name = message.Name,
            IsDeleted = existing?.IsDeleted ?? false,
            LastUpdatedAt = message.UpdatedAt,
            TenantId = message.OrganizationId
        });
        await session.SaveChangesAsync();
    }
}
