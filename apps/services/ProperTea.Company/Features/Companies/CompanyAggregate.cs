using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Company.Features.Companies.CompanyEvents;

namespace ProperTea.Company.Features.Companies;

public class CompanyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    // External IdP Organization ID
    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(Guid id, string code, string name, DateTimeOffset createdAt)
    {
        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_NAME_REQUIRED,
                "Company name is required");

        return new Created(id, code, name, createdAt);
    }

    public CodeUpdated UpdateCode(string code)
    {
        EnsureNotDeleted();
        ValidateCode(code);
        return new CodeUpdated(Id, code);
    }

    public NameUpdated UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));

        EnsureNotDeleted();
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
        Code = e.Code;
        Name = e.Name;
        CreatedAt = e.CreatedAt;
        CurrentStatus = Status.Active;
    }

    public void Apply(CodeUpdated e)
    {
        Code = e.Code;
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

    private void EnsureNotDeleted()
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_ALREADY_DELETED,
                "Cannot update a deleted company");
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_CODE_REQUIRED,
                "Company code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                CompanyErrorCodes.COMPANY_CODE_TOO_LONG,
                "Company code cannot exceed 50 characters");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
