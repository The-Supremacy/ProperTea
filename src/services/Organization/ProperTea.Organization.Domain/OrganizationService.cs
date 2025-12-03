using ProperTea.Core.Exceptions;
using ProperTea.Utilities;

namespace ProperTea.Organization.Domain;

public class OrganizationService(IOrganizationRepository repository)
{
    public async Task<Organization> CreateNewOrganizationAsync(string name)
    {
        if (!await repository.IsNameUniqueAsync(name).ConfigureAwait(false))
        {
            throw new DomainValidationException("An organization with this name already exists.");
        }

        var alias = SlugGenerator.Generate(name);
        return Organization.Create(name, alias);
    }
}
