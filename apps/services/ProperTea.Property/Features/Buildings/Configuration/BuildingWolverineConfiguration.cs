using ProperTea.Property.Extensions;
using Wolverine;

namespace ProperTea.Property.Features.Buildings.Configuration;

public static class BuildingWolverineConfiguration
{
    public static void ConfigureBuildingIntegrationEvents(this WolverineOptions opts)
    {
        opts.PublishIntegrationEvent<BuildingIntegrationEvents.BuildingCreated>("property.events");
        opts.PublishIntegrationEvent<BuildingIntegrationEvents.BuildingUpdated>("property.events");
        opts.PublishIntegrationEvent<BuildingIntegrationEvents.BuildingDeleted>("property.events");
    }
}
