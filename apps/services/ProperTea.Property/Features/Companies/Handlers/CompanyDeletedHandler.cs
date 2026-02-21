using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyDeletedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyDeleted message,
        IDocumentStore store)
    {
        await using var session = store.LightweightSession(message.OrganizationId);

        var existing = await session.LoadAsync<CompanyReference>(message.CompanyId);
        if (existing != null && existing.LastUpdatedAt >= message.DeletedAt)
            return;

        session.Store(new CompanyReference
        {
            Id = message.CompanyId,
            Code = existing?.Code ?? string.Empty,
            Name = existing?.Name ?? string.Empty,
            IsDeleted = true,
            LastUpdatedAt = message.DeletedAt,
            TenantId = message.OrganizationId
        });
        await session.SaveChangesAsync();
    }
}
