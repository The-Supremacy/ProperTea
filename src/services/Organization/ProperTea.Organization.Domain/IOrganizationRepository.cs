namespace ProperTea.Organization.Domain;

public interface IOrganizationRepository
{
    Task<bool> IsNameUniqueAsync(string name);

    Task<bool> IsAliasUniqueAsync(string orgAlias);
}
