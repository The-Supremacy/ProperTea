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
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string UnitNumber { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int? Floor { get; set; }
        public decimal? SquareFootage { get; set; }
        public int? RoomCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("units.updated.v1")]
    public class UnitUpdated : IUnitUpdated
    {
        public Guid UnitId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid? BuildingId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string UnitNumber { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int? Floor { get; set; }
        public decimal? SquareFootage { get; set; }
        public int? RoomCount { get; set; }
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
