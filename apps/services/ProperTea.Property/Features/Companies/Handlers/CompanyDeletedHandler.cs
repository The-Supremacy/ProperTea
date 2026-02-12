using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyDeletedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyDeleted message,
        IDocumentSession session)
    {
        var reference = await session.LoadAsync<CompanyReference>(message.CompanyId);

        if (reference != null)
        {
            reference.IsDeleted = true;
            reference.LastUpdatedAt = message.DeletedAt;
            session.Update(reference);
            await session.SaveChangesAsync();
        }
    }
}
