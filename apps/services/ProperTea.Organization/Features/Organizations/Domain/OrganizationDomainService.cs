using Marten;

namespace ProperTea.Organization.Features.Organizations.Domain;

/// <summary>
/// Domain service for organization operations requiring cross-aggregate logic or external queries.
/// Use for validations that need IDocumentSession before calling aggregate methods.
/// </summary>
public class OrganizationDomainService
{
    private readonly IDocumentSession _session;

    public OrganizationDomainService(IDocumentSession session)
    {
        _session = session;
    }

    // TODO: Add domain service methods
    // Example: ValidateUniqueNameAsync(name) - queries DB to check uniqueness
    // Then handler calls aggregate.Create() with validated data
}
