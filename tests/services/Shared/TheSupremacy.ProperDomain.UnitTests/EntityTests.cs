using Shouldly;

namespace TheSupremacy.ProperDomain.UnitTests;

public class EntityTests
{
    private class TestEntity : Entity
    {
        public TestEntity() : base()
        {
        }
        
        public TestEntity(Guid id) : base(id)
        {
        }
    }

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        id.ShouldBe(entity.Id);
    }

    [Fact]
    public void ParameterlessConstructor_ForEfCore_CreatesEntity()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
    }
}