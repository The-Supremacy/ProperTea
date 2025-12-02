namespace ProperTea.Organization.Domain;

public interface IOrganizationRepository
{
    Task<bool> IsNameUniqueAsync(string name);
}
