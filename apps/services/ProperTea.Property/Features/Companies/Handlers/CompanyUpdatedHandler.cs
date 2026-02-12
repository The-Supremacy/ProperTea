using Marten;
using ProperTea.Contracts.Events;
using Wolverine;

namespace ProperTea.Property.Features.Companies.Handlers;

public class CompanyUpdatedHandler : IWolverineHandler
{
    public async Task Handle(
        ICompanyUpdated message,
        IDocumentSession session)
    {
        var reference = await session.LoadAsync<CompanyReference>(message.CompanyId);

        if (reference == null)
        {
            // Company not tracked yet - create it
            reference = new CompanyReference
            {
                Id = message.CompanyId,
                Code = message.Code,
                Name = message.Name,
                IsDeleted = false,
                LastUpdatedAt = message.UpdatedAt,
                TenantId = message.OrganizationId.ToString()
            };
            session.Store(reference);
        }
        else
        {
            reference.Code = message.Code;
            reference.Name = message.Name;
            reference.LastUpdatedAt = message.UpdatedAt;
            session.Update(reference);
        }

        await session.SaveChangesAsync();
    }
}
