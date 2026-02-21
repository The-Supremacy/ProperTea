using Marten;
using Marten.Events.Projections;

namespace ProperTea.Property.Features.Units.Configuration;

public static class UnitMartenConfiguration
{
    public static void ConfigureUnitMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<UnitAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<UnitAggregate>()
            .Index(x => x.PropertyId)
            .Index(x => x.BuildingId)
            .Index(x => x.Code)
            .Index(x => x.UnitReference)
            .Index(x => x.Category)
            .Index(x => x.Floor)
            .Index(x => x.CurrentStatus);

        opts.Events.MapEventType<UnitEvents.Created>("unit.created.v1");
        opts.Events.MapEventType<UnitEvents.CodeUpdated>("unit.code-updated.v1");
        opts.Events.MapEventType<UnitEvents.UnitReferenceRegenerated>("unit.reference-regenerated.v1");
        opts.Events.MapEventType<UnitEvents.CategoryChanged>("unit.category-changed.v1");
        opts.Events.MapEventType<UnitEvents.LocationChanged>("unit.location-changed.v1");
        opts.Events.MapEventType<UnitEvents.AddressUpdated>("unit.address-updated.v1");
        opts.Events.MapEventType<UnitEvents.FloorUpdated>("unit.floor-updated.v1");
        opts.Events.MapEventType<UnitEvents.Deleted>("unit.deleted.v1");
    }
}
