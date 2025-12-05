namespace ProperTea.Organization.Domain;

public interface IOrganizationRepository
{
    public Task<bool> IsNameUniqueAsync(string name);

    public Task<bool> IsAliasUniqueAsync(string orgAlias);
}
