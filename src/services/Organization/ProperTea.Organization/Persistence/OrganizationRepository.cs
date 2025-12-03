using Microsoft.EntityFrameworkCore;
using ProperTea.Organization.Domain;

namespace ProperTea.Organization.Persistence;

public class OrganizationRepository(OrganizationDbContext dbContext) : IOrganizationRepository
{
    public Task<bool> IsNameUniqueAsync(string name)
    {
        return dbContext.Organizations.AllAsync(o => o.Name != name);
    }

    public Task<bool> IsAliasUniqueAsync(string orgAlias)
    {
        return dbContext.Organizations.AllAsync(o => o.OrgAlias != orgAlias);
    }
}
