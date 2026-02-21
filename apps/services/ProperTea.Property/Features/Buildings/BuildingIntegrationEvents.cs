using ProperTea.Contracts.Events;
using Wolverine.Attributes;

namespace ProperTea.Property.Features.Buildings;

public static class BuildingIntegrationEvents
{
    [MessageIdentity("buildings.created.v1")]
    public class BuildingCreated : IBuildingCreated
    {
        public Guid BuildingId { get; set; }
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }

    [MessageIdentity("buildings.updated.v1")]
    public class BuildingUpdated : IBuildingUpdated
    {
        public Guid BuildingId { get; set; }
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public AddressData Address { get; set; } = null!;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [MessageIdentity("buildings.deleted.v1")]
    public class BuildingDeleted : IBuildingDeleted
    {
        public Guid BuildingId { get; set; }
        public Guid PropertyId { get; set; }
        public string OrganizationId { get; set; } = null!;
        public DateTimeOffset DeletedAt { get; set; }
    }
}
