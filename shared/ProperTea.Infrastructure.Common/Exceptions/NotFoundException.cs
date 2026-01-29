namespace ProperTea.Infrastructure.Common.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string resourceType, object resourceId)
        : base(errorCode, $"{resourceType} with ID '{resourceId}' was not found", new Dictionary<string, object>
        {
            ["resourceType"] = resourceType,
            ["resourceId"] = resourceId
        })
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string errorCode, string message, Dictionary<string, object>? parameters = null)
        : base(errorCode, message, parameters)
    {
    }

    public string? ResourceType { get; }
    public object? ResourceId { get; }
}

