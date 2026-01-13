namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when a requested resource (aggregate, entity) is not found.
/// </summary>
public class NotFoundException(string resourceType, object resourceId)
    : DomainException($"{resourceType} with ID '{resourceId}' was not found")
{
    public string ResourceType { get; } = resourceType;
    public object ResourceId { get; } = resourceId;
}
