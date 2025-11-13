namespace TheSupremacy.ProperIntegrationEvents;

public interface IIntegrationEventTypeResolver
{
    Type? ResolveType(string eventTypeName);
}

public class IntegrationEventTypeResolver(IReadOnlyDictionary<string, Type> eventTypes) : IIntegrationEventTypeResolver
{
    public Type? ResolveType(string eventTypeName)
    {
        return eventTypes.GetValueOrDefault(eventTypeName);
    }
}