namespace TheSupremacy.ProperIntegrationEvents;

public interface IMessageTransport
{
    Task SendAsync(
        string topic,
        string eventType,
        string payload,
        Dictionary<string, string> headers,
        CancellationToken ct = default);
}