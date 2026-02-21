using Marten.Metadata;
using ProperTea.Infrastructure.Common.Address;
using ProperTea.Infrastructure.Common.Exceptions;
using ProperTea.Infrastructure.Common.Validation;
using static ProperTea.Property.Features.Buildings.BuildingEvents;

namespace ProperTea.Property.Features.Buildings;

public class BuildingAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public List<Entrance> Entrances { get; set; } = [];
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }

    public string? TenantId { get; set; }

    public static Created Create(
        Guid id,
        Guid propertyId,
        string code,
        string name,
        Address address,
        DateTimeOffset createdAt)
    {
        if (propertyId == Guid.Empty)
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_PROPERTY_REQUIRED,
                "Building must belong to a property");

        ValidateCode(code);
        ValidateAddress(address);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_NAME_REQUIRED,
                "Building name is required");

        return new Created(id, propertyId, code, name, address, createdAt);
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

    public AddressUpdated UpdateAddress(Address address)
    {
        EnsureNotDeleted();
        ValidateAddress(address);
        return new AddressUpdated(Id, address);
    }

    public EntranceAdded AddEntrance(string code, string name)
    {
        EnsureNotDeleted();
        ValidateEntranceCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_ENTRANCE_NAME_REQUIRED,
                "Entrance name is required");

        if (Entrances.Any(e => e.Code == code))
            throw new ConflictException(
                BuildingErrorCodes.BUILDING_ENTRANCE_CODE_ALREADY_EXISTS,
                $"An entrance with code '{code}' already exists in this building");

        return new EntranceAdded(Id, Guid.NewGuid(), code, name);
    }

    public EntranceUpdated UpdateEntrance(Guid entranceId, string code, string name)
    {
        EnsureNotDeleted();

        var entrance = Entrances.FirstOrDefault(e => e.Id == entranceId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_ENTRANCE_NOT_FOUND,
                "Entrance",
                entranceId);

        ValidateEntranceCode(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessViolationException(
                BuildingErrorCodes.BUILDING_ENTRANCE_NAME_REQUIRED,
                "Entrance name is required");

        if (Entrances.Any(e => e.Id != entranceId && e.Code == code))
            throw new ConflictException(
                BuildingErrorCodes.BUILDING_ENTRANCE_CODE_ALREADY_EXISTS,
                $"An entrance with code '{code}' already exists in this building");

        return new EntranceUpdated(Id, entranceId, code, name);
    }

    public EntranceRemoved RemoveEntrance(Guid entranceId)
    {
        EnsureNotDeleted();

        _ = Entrances.FirstOrDefault(e => e.Id == entranceId)
            ?? throw new NotFoundException(
                BuildingErrorCodes.BUILDING_ENTRANCE_NOT_FOUND,
                "Entrance",
                entranceId);

        return new EntranceRemoved(Id, entranceId);
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

    public void Apply(EntranceAdded e)
    {
        Entrances.Add(new Entrance(e.EntranceId, e.Code, e.Name));
    }

    public void Apply(EntranceUpdated e)
    {
        Entrances = [.. Entrances.Select(x =>
            x.Id == e.EntranceId ? x with { Code = e.Code, Name = e.Name } : x)];
    }

    public void Apply(EntranceRemoved e)
    {
        Entrances = [.. Entrances.Where(x => x.Id != e.EntranceId)];
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
        CodeValidator.Validate(
            code,
            maxLength: 5,
            errorRequired: BuildingErrorCodes.BUILDING_CODE_REQUIRED,
            errorTooLong: BuildingErrorCodes.BUILDING_CODE_TOO_LONG,
            errorInvalidFormat: BuildingErrorCodes.BUILDING_CODE_INVALID_FORMAT);
    }

    private static void ValidateEntranceCode(string code)
    {
        CodeValidator.Validate(
            code,
            maxLength: 5,
            errorRequired: BuildingErrorCodes.BUILDING_ENTRANCE_CODE_REQUIRED,
            errorTooLong: BuildingErrorCodes.BUILDING_ENTRANCE_CODE_TOO_LONG,
            errorInvalidFormat: BuildingErrorCodes.BUILDING_ENTRANCE_CODE_INVALID_FORMAT);
    }

    private static void ValidateAddress(Address address)
    {
        // Address is optional for buildings; no strict field validation.
    }

    public enum Status
    {
        Active = 1,
        Deleted = 2
    }
}
