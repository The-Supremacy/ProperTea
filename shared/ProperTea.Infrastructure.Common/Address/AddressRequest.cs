namespace ProperTea.Infrastructure.Common.Address;

public record AddressRequest(string Country, string City, string ZipCode, string StreetAddress)
{
    public Address ToAddress()
    {
        if (!Enum.TryParse<Country>(Country, ignoreCase: true, out var country))
            throw new ArgumentException($"'{Country}' is not a recognised country code", nameof(Country));

        return new Address(country, City, ZipCode, StreetAddress);
    }
}
