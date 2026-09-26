using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class LegacyHistoryEventTypeMapperTests
{
    [Fact]
    public void TryMap_ShouldExplicitlyCoverEveryParkEventType()
    {
        foreach (ParkHistoryEventType eventType in Enum.GetValues<ParkHistoryEventType>())
        {
            bool mapped = LegacyHistoryEventTypeMapper.TryMap(
                HistoryEntityType.Park,
                eventType.ToString(),
                out LegacyHistoryEventTypeMapping? mapping);

            Assert.True(mapped, $"Park event type {eventType} is not mapped.");
            Assert.NotNull(mapping);
        }
    }

    [Fact]
    public void TryMap_ShouldExplicitlyCoverEveryParkItemEventType()
    {
        foreach (ParkItemHistoryEventType eventType in Enum.GetValues<ParkItemHistoryEventType>())
        {
            bool mapped = LegacyHistoryEventTypeMapper.TryMap(
                HistoryEntityType.ParkItem,
                eventType.ToString(),
                out LegacyHistoryEventTypeMapping? mapping);

            Assert.True(mapped, $"Park-item event type {eventType} is not mapped.");
            Assert.NotNull(mapping);
        }
    }

    [Fact]
    public void TryMap_WhenEventTypeIsUnknown_ShouldNotSilentlyUseOther()
    {
        bool mapped = LegacyHistoryEventTypeMapper.TryMap(
            HistoryEntityType.Park,
            "UnexpectedLegacyValue",
            out LegacyHistoryEventTypeMapping? mapping);

        Assert.False(mapped);
        Assert.Null(mapping);
    }

    [Fact]
    public void TryMap_WhenLogoChanges_ShouldPreserveLogoBoundary()
    {
        bool mapped = LegacyHistoryEventTypeMapper.TryMap(
            HistoryEntityType.Park,
            ParkHistoryEventType.LogoChange.ToString(),
            out LegacyHistoryEventTypeMapping? mapping);

        Assert.True(mapped);
        Assert.Equal(HistoricalFactType.LogoChange, mapping!.FactType);
        Assert.Equal(HistoricalAttributeKind.Logo, mapping.AttributeKind);
        Assert.Equal(AttributeBoundaryMeaning.Unspecified, mapping.AttributeBoundaryMeaning);
    }
}
