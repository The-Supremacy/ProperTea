using Shouldly;

namespace TheSupremacy.ProperDomain.UnitTests;

public class ValueObjectTests
{
    private record Address(string Street, string City, string ZipCode) : ValueObject;
    private record Money(decimal Amount, string Currency) : ValueObject;

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address1.ShouldBe(address2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act & Assert
        address1.ShouldNotBe(address2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }
    
    [Fact]
    public void ValueObject_SupportsStructuralEquality()
    {
        // Arrange
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");
        var money3 = new Money(100.00m, "EUR");

        // Act & Assert
        money1.ShouldBe(money2);
        money1.ShouldNotBe(money3);
    }
}