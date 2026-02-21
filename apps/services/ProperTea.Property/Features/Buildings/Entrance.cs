namespace ProperTea.Property.Features.Buildings;

/// <summary>
/// An entrance to a Building (staircase, lobby, wing entry point).
/// Value object — owned by and always loaded as part of BuildingAggregate.
/// </summary>
public record Entrance(Guid Id, string Code, string Name);
