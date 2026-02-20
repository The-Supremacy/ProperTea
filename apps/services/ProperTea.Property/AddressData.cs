using ProperTea.Contracts.Events;

namespace ProperTea.Property;

public record AddressData(string Country, string City, string ZipCode, string StreetAddress) : IAddressData;
