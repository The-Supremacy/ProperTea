using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Property.Features.Properties.PropertyEvents;

namespace ProperTea.Property.Features.Properties;

public class PropertyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    #region Factory Methods

    public static Created Create(
        Guid id,
        Guid companyId,
        string code,
        string name,
        string address,
        DateTimeOffset createdAt)
    {
        if (companyId == Guid.Empty)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_COMPANY_REQUIRED,
                "Property must be owned by a company");

        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_NAME_REQUIRED,
                "Property name is required");

        if (string.IsNullOrWhiteSpace(address))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ADDRESS_REQUIRED,
                "Property address is required");

        return new Created(id, companyId, code, name, address, createdAt);
    }

    public CodeUpdated UpdateCode(string code)
    {
        EnsureNotDeleted();
        ValidateCode(code);
        return new CodeUpdated(Id, code);
    }

    public NameUpdated UpdateName(string name)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_NAME_REQUIRED,
                "Property name is required");

        return new NameUpdated(Id, name);
    }

    public AddressUpdated UpdateAddress(string address)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(address))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ADDRESS_REQUIRED,
                "Property address is required");

        return new AddressUpdated(Id, address);
    }

    public Deleted Delete(DateTimeOffset deletedAt)
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_ALREADY_DELETED,
                "Property is already deleted");

        return new Deleted(Id, deletedAt);
    }

    #endregion

    #region Event Appliers

    public void Apply(Created e)
    {
        Id = e.PropertyId;
        CompanyId = e.CompanyId;
        Code = e.Code;
        Name = e.Name;
        Address = e.Address;
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

    public void Apply(AddressUpdated e)
    {
        Address = e.Address;
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
                PropertyErrorCodes.PROPERTY_ALREADY_DELETED,
                "Cannot modify a deleted property");
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_CODE_REQUIRED,
                "Property code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_CODE_TOO_LONG,
                "Property code cannot exceed 50 characters");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
