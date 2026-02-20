using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Units;

public static class UnitIntegrationEvents
{
    [MessageIdentity("units.created.v1")]
    public class UnitCreated : IUnitCreated
    {
        public Guid UnitId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid? BuildingId { get; set; }
        public Guid? EntranceId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string UnitReference { get; set; } = null!;
        public string Category { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public int? Floor { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        IAddressData IUnitCreated.Address => Address;
    }

    [MessageIdentity("units.updated.v1")]
    public class UnitUpdated : IUnitUpdated
    {
        public Guid UnitId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid? BuildingId { get; set; }
        public Guid? EntranceId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string UnitReference { get; set; } = null!;
        public string Category { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public int? Floor { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        IAddressData IUnitUpdated.Address => Address;
    }

    [MessageIdentity("units.deleted.v1")]
    public class UnitDeleted : IUnitDeleted
    {
        public Guid UnitId { get; set; }
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public DateTimeOffset DeletedAt { get; set; }
    }
}
