namespace ProperTea.ProperIntegrationEvents.Outbox;

public interface IIntegrationEventTypeResolver
{
    Type? ResolveType(string eventTypeName);
}

public class IntegrationEventTypeResolver : IIntegrationEventTypeResolver
{
    private readonly IReadOnlyDictionary<string, Type> _eventTypes;

    public IntegrationEventTypeResolver(IReadOnlyDictionary<string, Type> eventTypes)
    {
        _eventTypes = eventTypes;
    }

    public Type? ResolveType(string eventTypeName)
    {
        return _eventTypes.GetValueOrDefault(eventTypeName);
    }
}
