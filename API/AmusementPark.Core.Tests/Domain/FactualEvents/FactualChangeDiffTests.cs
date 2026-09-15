using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class FactualChangeDiffTests
{
    [Fact]
    public void Detect_WithEquivalentStructuredValues_ShouldReturnNoChange()
    {
        FactValue previousValue = FactValue.FromMoney(49.90m, "eur");
        FactValue newValue = FactValue.FromMoney(49.900m, "EUR");

        FactualChangeDiff? diff = FactualChangeDiff.Detect(previousValue, newValue);

        Assert.Null(diff);
    }

    [Fact]
    public void Detect_WithChangedStructuredValue_ShouldPreserveBeforeAndAfter()
    {
        FactValue previousValue = FactValue.FromDate(new DateOnly(2026, 4, 1));
        FactValue newValue = FactValue.FromDate(new DateOnly(2026, 4, 3));

        FactualChangeDiff? diff = FactualChangeDiff.Detect(previousValue, newValue);

        Assert.NotNull(diff);
        Assert.Equal(previousValue, diff.PreviousValue);
        Assert.Equal(newValue, diff.NewValue);
    }

    [Fact]
    public void Detect_WithCreatedOrRemovedFact_ShouldReturnAChange()
    {
        FactValue value = FactValue.FromText("Nouvelle valeur");

        Assert.NotNull(FactualChangeDiff.Detect(null, value));
        Assert.NotNull(FactualChangeDiff.Detect(value, null));
        Assert.Null(FactualChangeDiff.Detect(null, null));
    }
}
