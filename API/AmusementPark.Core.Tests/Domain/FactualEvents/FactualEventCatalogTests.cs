using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class FactualEventCatalogTests
{
    [Fact]
    public void All_ShouldVersionEveryDeclaredEventTypeExactlyOnce()
    {
        FactualEventType[] declaredTypes = Enum.GetValues<FactualEventType>();

        Assert.Equal(declaredTypes.Length, FactualEventCatalog.All.Count);
        Assert.Equal(
            declaredTypes.Order(),
            FactualEventCatalog.All.Keys.Order());
        Assert.Equal(
            FactualEventCatalog.All.Count,
            FactualEventCatalog.All.Values.Select(static definition => definition.Code)
                .Distinct(StringComparer.Ordinal).Count());
        Assert.All(
            FactualEventCatalog.All.Values,
            static definition => Assert.Equal(
                FactualEventCatalog.CurrentSchemaVersion,
                definition.SchemaVersion));
    }

    [Theory]
    [InlineData(FactualEventType.ParkNameChanged, FactualTargetType.Park, true)]
    [InlineData(FactualEventType.ParkNameChanged, FactualTargetType.ParkItem, false)]
    [InlineData(FactualEventType.OpeningDateConfirmed, FactualTargetType.Park, false)]
    [InlineData(FactualEventType.OpeningDateConfirmed, FactualTargetType.ParkItem, true)]
    [InlineData(FactualEventType.MajorHistoryUpdate, FactualTargetType.Park, true)]
    [InlineData(FactualEventType.MajorHistoryUpdate, FactualTargetType.ParkItem, true)]
    public void Get_ShouldExposeExplicitTargetCompatibility(
        FactualEventType type,
        FactualTargetType targetType,
        bool expected)
    {
        FactualEventDefinition definition = FactualEventCatalog.Get(type);

        Assert.Equal(expected, definition.Supports(targetType));
    }

    [Fact]
    public void Get_WithUnknownType_ShouldRejectUnversionedEvent()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactualEventCatalog.Get((FactualEventType)999));

        Assert.Equal(FactualEventErrorCodes.InvalidType, exception.Code);
    }
}
