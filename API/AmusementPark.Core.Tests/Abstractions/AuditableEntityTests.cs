using AmusementPark.Core.Abstractions;
using Xunit;

namespace AmusementPark.Core.Tests.Abstractions;

public sealed class AuditableEntityTests
{
    [Fact]
    public void Constructor_WhenEntityIsCreated_ShouldInitializeAuditDatesInUtcWindow()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        TestAuditableEntity entity = new TestAuditableEntity();

        DateTime after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(entity.CreatedAtUtc, before, after);
        Assert.InRange(entity.UpdatedAtUtc, before, after);
    }

    [Fact]
    public void Touch_WhenCalled_ShouldMoveUpdatedDateForwardWithoutChangingCreatedDate()
    {
        TestAuditableEntity entity = new TestAuditableEntity();
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        DateTime previousUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        entity.CreatedAtUtc = createdAtUtc;
        entity.UpdatedAtUtc = previousUpdatedAtUtc;

        entity.Touch();

        Assert.Equal(createdAtUtc, entity.CreatedAtUtc);
        Assert.True(entity.UpdatedAtUtc > previousUpdatedAtUtc);
    }

    private sealed class TestAuditableEntity : AuditableEntity
    {
    }
}
