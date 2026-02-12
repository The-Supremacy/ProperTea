using Marten;
using Marten.Events.Projections;

namespace ProperTea.Property.Features.Properties.Configuration;

public static class PropertyMartenConfiguration
{
    public static void ConfigurePropertyMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<PropertyAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<PropertyAggregate>()
            .Index(x => x.Code)
            .Index(x => x.Name)
            .Index(x => x.CompanyId)
            .Index(x => x.CurrentStatus);

        opts.Events.MapEventType<PropertyEvents.Created>("property.created.v1");
        opts.Events.MapEventType<PropertyEvents.Updated>("property.updated.v1");
        opts.Events.MapEventType<PropertyEvents.Deleted>("property.deleted.v1");
        opts.Events.MapEventType<PropertyEvents.BuildingAdded>("property.building-added.v1");
        opts.Events.MapEventType<PropertyEvents.BuildingUpdated>("property.building-updated.v1");
        opts.Events.MapEventType<PropertyEvents.BuildingRemoved>("property.building-removed.v1");
    }
}
