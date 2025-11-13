using Shouldly;
using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.UnitTests.Domain;

public class SagaValidationResultTests
{
    [Fact]
    public void SagaValidationResult_Success_IsValid()
    {
        // Act
        var result = SagaValidationResult.Success();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void SagaValidationResult_Failure_IsNotValid()
    {
        // Act
        var result = SagaValidationResult.Failure("Error");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Error");
    }
}