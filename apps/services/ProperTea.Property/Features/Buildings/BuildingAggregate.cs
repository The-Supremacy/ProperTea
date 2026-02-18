using Marten.Metadata;
using ProperTea.Infrastructure.Common.Exceptions;
using static ProperTea.Property.Features.Buildings.BuildingEvents;

namespace ProperTea.Property.Features.Buildings;

public class BuildingAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    public static Created Create(
        Guid id,
        Guid propertyId,
        string code,
        string name,
        DateTimeOffset createdAt)
    {
        if (propertyId == Guid.Empty)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_PROPERTY_REQUIRED,
                "Building must belong to a property");

        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_NAME_REQUIRED,
                "Building name is required");

        return new Created(id, propertyId, code, name, createdAt);
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
                BuildingErrorCodes.BUILDING_NAME_REQUIRED,
                "Building name is required");

        return new NameUpdated(Id, name);
    }

    public Deleted Delete(DateTimeOffset deletedAt)
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_ALREADY_DELETED,
                "Building is already deleted");

        return new Deleted(Id, deletedAt);
    }

    public void Apply(Created e)
    {
        Id = e.BuildingId;
        PropertyId = e.PropertyId;
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

    private void EnsureNotDeleted()
    {
        if (CurrentStatus == Status.Deleted)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_ALREADY_DELETED,
                "Cannot modify a deleted building");
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_CODE_REQUIRED,
                "Building code is required");

        if (code.Length > 50)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_CODE_TOO_LONG,
                "Building code cannot exceed 50 characters");
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}