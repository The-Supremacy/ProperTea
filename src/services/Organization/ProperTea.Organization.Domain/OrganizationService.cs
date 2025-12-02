using ProperTea.Core.Exceptions;

namespace ProperTea.Organization.Domain;

public class OrganizationService(IOrganizationRepository repository)
{
    public async Task<Organization> CreateNewOrganizationAsync(string name)
    {
        if (!await repository.IsNameUniqueAsync(name).ConfigureAwait(false))
        {
            throw new DomainValidationException("An organization with this name already exists.");
        }

        return Organization.Create(name);
    }
}
