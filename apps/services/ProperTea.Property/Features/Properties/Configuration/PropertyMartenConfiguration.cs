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
        opts.Events.MapEventType<PropertyEvents.CodeUpdated>("property.code-updated.v1");
        opts.Events.MapEventType<PropertyEvents.NameUpdated>("property.name-updated.v1");
        opts.Events.MapEventType<PropertyEvents.AddressUpdated>("property.address-updated.v1");
        opts.Events.MapEventType<PropertyEvents.Deleted>("property.deleted.v1");
    }
}
