using Marten.Metadata;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Infrastructure.Common.Validation;
using static ProperTea.Property.Features.Properties.PropertyEvents;

namespace ProperTea.Property.Features.Properties;

public class PropertyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Address Address { get; set; } = null!;
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
        Address address,
        DateTimeOffset createdAt)
    {
        if (companyId == Guid.Empty)
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_COMPANY_REQUIRED,
                "Property must be owned by a company");

        ValidateCode(code);
        ValidateAddress(address);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                PropertyErrorCodes.PROPERTY_NAME_REQUIRED,
                "Property name is required");

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

    public AddressUpdated UpdateAddress(Address address)
    {
        EnsureNotDeleted();
        ValidateAddress(address);
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
        CodeValidator.Validate(
            code,
            maxLength: 10,
            errorRequired: PropertyErrorCodes.PROPERTY_CODE_REQUIRED,
            errorTooLong: PropertyErrorCodes.PROPERTY_CODE_TOO_LONG,
            errorInvalidFormat: PropertyErrorCodes.PROPERTY_CODE_INVALID_FORMAT);
    }

    private static void ValidateAddress(Address address)
    {
        // Address is optional for properties; no strict field validation.
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
