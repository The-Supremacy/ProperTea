using ProperTea.Property.Extensions;
using Wolverine;

namespace ProperTea.Property.Features.Units.Configuration;

public static class UnitWolverineConfiguration
{
    public static void ConfigureUnitIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<UnitIntegrationEvents.UnitCreated>("property.events");
        opts.PublishIntegrationEvent<UnitIntegrationEvents.UnitUpdated>("property.events");
        opts.PublishIntegrationEvent<UnitIntegrationEvents.UnitDeleted>("property.events");
    }
}
