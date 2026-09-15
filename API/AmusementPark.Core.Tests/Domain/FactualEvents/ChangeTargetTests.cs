using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class ChangeTargetTests
{
    [Fact]
    public void ForParkItem_ShouldKeepItsNormalizedParentContext()
    {
        ChangeTarget target = ChangeTarget.ForParkItem(" item-1 ", " park-1 ");

        Assert.Equal(FactualTargetType.ParkItem, target.Type);
        Assert.Equal("item-1", target.TargetId);
        Assert.Equal("park-1", target.ParentParkId);
    }

    [Fact]
    public void Restore_ForParkWithParent_ShouldRejectContradictoryContext()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => ChangeTarget.Restore(FactualTargetType.Park, "park-1", "park-parent"));

        Assert.Equal(FactualEventErrorCodes.InvalidTarget, exception.Code);
    }

    [Fact]
    public void Restore_ForParkItemWithoutParent_ShouldRejectAmbiguousContext()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => ChangeTarget.Restore(FactualTargetType.ParkItem, "item-1", null));

        Assert.Equal(FactualEventErrorCodes.InvalidTarget, exception.Code);
    }

    [Fact]
    public void Restore_WithUnknownTargetType_ShouldRejectTarget()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => ChangeTarget.Restore((FactualTargetType)99, "target-1", null));

        Assert.Equal(FactualEventErrorCodes.InvalidTarget, exception.Code);
    }
}
