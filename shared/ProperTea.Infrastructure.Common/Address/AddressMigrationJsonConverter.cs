using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProperTea.Infrastructure.Common.Address;

/// <summary>
/// Handles reading Address values that were persisted as plain strings in older snapshots/events.
/// When a string token is encountered, the raw value is treated as the street address with
/// placeholder values for the structured fields. New data is read/written as an object.
/// </summary>
public sealed class AddressMigrationJsonConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Legacy format: Address was stored as a plain string before the structured type was introduced.
            var raw = reader.GetString() ?? string.Empty;
            return new Address(Country.UA, string.Empty, string.Empty, raw);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var countryStr = root.TryGetProperty("Country", out var c) ? c.GetString() : null;
            var city = root.TryGetProperty("City", out var ci) ? ci.GetString() ?? string.Empty : string.Empty;
            var zip = root.TryGetProperty("ZipCode", out var z) ? z.GetString() ?? string.Empty : string.Empty;
            var street = root.TryGetProperty("StreetAddress", out var s) ? s.GetString() ?? string.Empty : string.Empty;

            var country = Enum.TryParse<Country>(countryStr, ignoreCase: true, out var parsed) ? parsed : Country.UA;
            return new Address(country, city, zip, street);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when reading Address.");
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Country", value.Country.ToString());
        writer.WriteString("City", value.City);
        writer.WriteString("ZipCode", value.ZipCode);
        writer.WriteString("StreetAddress", value.StreetAddress);
        writer.WriteEndObject();
    }
}
