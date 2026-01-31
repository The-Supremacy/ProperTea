using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Company.Features.Companies.CompanyEvents;

namespace ProperTea.Company.Features.Companies;

public class CompanyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    // External IdP Organization ID
    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(Guid id, string name, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));

        return new Created(id, name, createdAt);
    }

    public NameUpdated UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));

        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_ALREADY_DELETED,
                "Cannot update a deleted company");

        return new NameUpdated(Id, name);
    }

    public Deleted Delete(DateTimeOffset deletedAt)
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_ALREADY_DELETED,
                "Company is already deleted");

        return new Deleted(Id, deletedAt);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.CompanyId;
        Name = e.Name;
        CreatedAt = e.CreatedAt;
        CurrentStatus = Status.Active;
    }

    public void Apply(NameUpdated e)
    {
        Name = e.Name;
    }

    public void Apply(Deleted e)
    {
        CurrentStatus = Status.Deleted;
    }

    #endregion

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
