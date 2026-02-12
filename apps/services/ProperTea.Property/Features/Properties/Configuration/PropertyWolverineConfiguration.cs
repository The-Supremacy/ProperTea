using ProperTea.Property.Extensions;
using Wolverine;

namespace ProperTea.Property.Features.Properties.Configuration;

public static class PropertyWolverineConfiguration
{
    public static void ConfigurePropertyIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<PropertyIntegrationEvents.PropertyCreated>("property.events");
        opts.PublishIntegrationEvent<PropertyIntegrationEvents.PropertyUpdated>("property.events");
        opts.PublishIntegrationEvent<PropertyIntegrationEvents.PropertyDeleted>("property.events");

        // Route PropertyDeleted to local durable queue for internal cascade-delete of units
        _ = opts.PublishMessage<PropertyIntegrationEvents.PropertyDeleted>()
            .ToLocalQueue("unit-cascade-delete")
            .UseDurableInbox();
    }
}
