using AmusementPark.Core.Abstractions;
using Xunit;

namespace AmusementPark.Core.Tests.Abstractions;

public sealed class EntityBaseTests
{
    [Fact]
    public void Constructor_WhenEntityIsCreated_ShouldInitializeNonEmptyId()
    {
        TestEntity entity = new TestEntity();

        Assert.False(string.IsNullOrWhiteSpace(entity.Id));
    }

    [Fact]
    public void Constructor_WhenMultipleEntitiesAreCreated_ShouldInitializeDifferentIds()
    {
        TestEntity first = new TestEntity();
        TestEntity second = new TestEntity();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Id_WhenAssignedManually_ShouldKeepAssignedValue()
    {
        TestEntity entity = new TestEntity();

        entity.Id = "entity-1";

        Assert.Equal("entity-1", entity.Id);
    }

    private sealed class TestEntity : EntityBase
    {
    }
}
