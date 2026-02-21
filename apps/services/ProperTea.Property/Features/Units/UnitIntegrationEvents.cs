using ProperTea.Contracts.Events;
using ProperTea.Property.Features.Properties;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Units;

public static class UnitIntegrationEvents
{
    [MessageIdentity("units.created.v2")]
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
        IAddressData IUnitCreated.Address => Address;
        public int? Floor { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("units.updated.v2")]
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
        IAddressData IUnitUpdated.Address => Address;
        public int? Floor { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
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
