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
            .Index(x => x.UnitNumber)
            .Index(x => x.Category)
            .Index(x => x.Floor)
            .Index(x => x.CurrentStatus);

        opts.Events.MapEventType<UnitEvents.Created>("unit.created.v1");
        opts.Events.MapEventType<UnitEvents.Updated>("unit.updated.v1");
        opts.Events.MapEventType<UnitEvents.Deleted>("unit.deleted.v1");
    }
}
