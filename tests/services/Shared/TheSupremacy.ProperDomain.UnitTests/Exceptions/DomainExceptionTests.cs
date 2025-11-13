using Shouldly;
using TheSupremacy.ProperDomain.Exceptions;

namespace TheSupremacy.ProperDomain.UnitTests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Domain rule violated";

        // Act
        var exception = new DomainException(message);

        // Assert
        message.ShouldBe(exception.Message);
    }

    [Fact]
    public void ThrowDomainException_CanBeCaught()
    {
        // Act & Assert
        Should.Throw<DomainException>(() => throw new DomainException("Test"));
    }
}