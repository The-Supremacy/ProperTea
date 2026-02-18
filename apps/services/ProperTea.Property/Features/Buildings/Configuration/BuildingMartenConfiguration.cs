using Marten;
using Marten.Events.Projections;

namespace ProperTea.Property.Features.Buildings.Configuration;

public static class BuildingMartenConfiguration
{
    public static void ConfigureBuildingMarten(this StoreOptions opts)
    {
        _ = opts.Projections.Snapshot<BuildingAggregate>(SnapshotLifecycle.Inline);

        _ = opts.Schema.For<BuildingAggregate>()
            .Index(x => x.Code)
            .Index(x => x.Name)
            .Index(x => x.PropertyId)
            .Index(x => x.CurrentStatus);

        opts.Events.MapEventType<BuildingEvents.Created>("building.created.v1");
        opts.Events.MapEventType<BuildingEvents.CodeUpdated>("building.code-updated.v1");
        opts.Events.MapEventType<BuildingEvents.NameUpdated>("building.name-updated.v1");
        opts.Events.MapEventType<BuildingEvents.Deleted>("building.deleted.v1");
    }
}