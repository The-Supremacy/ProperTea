namespace ProperTea.Infrastructure.Common.Address;

/// <summary>
/// Immutable structured address value object.
/// Validation of each field is the responsibility of the owning aggregate's factory method.
/// </summary>
public record Address(
    Country Country,
    string City,
    string ZipCode,
    string StreetAddress);
