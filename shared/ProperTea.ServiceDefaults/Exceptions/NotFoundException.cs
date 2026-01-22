namespace ProperTea.ServiceDefaults.Exceptions;

/// <summary>
/// Exception thrown when a requested resource (aggregate, entity) is not found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public string? ResourceType { get; }
    public object? ResourceId { get; }
}

