using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalNarrativeTypeMapperTests
{
    [Fact]
    public void TryMap_ShouldExplicitlyCoverEveryAutomaticallyConvertibleParkEventType()
    {
        foreach (ParkHistoryEventType eventType in Enum.GetValues<ParkHistoryEventType>())
        {
            if (eventType == ParkHistoryEventType.SeasonOpening)
            {
                continue;
            }

            bool mapped = HistoricalNarrativeTypeMapper.TryMap(
                HistoryEntityType.Park,
                eventType.ToString(),
                out HistoricalNarrativeTypeMapping? mapping);

            Assert.True(mapped, $"Park event type {eventType} is not mapped.");
            Assert.NotNull(mapping);
        }
    }

    [Fact]
    public void TryMap_ShouldExplicitlyCoverEveryAutomaticallyConvertibleParkItemEventType()
    {
        foreach (ParkItemHistoryEventType eventType in Enum.GetValues<ParkItemHistoryEventType>())
        {
            if (eventType == ParkItemHistoryEventType.SeasonOpening)
            {
                continue;
            }

            bool mapped = HistoricalNarrativeTypeMapper.TryMap(
                HistoryEntityType.ParkItem,
                eventType.ToString(),
                out HistoricalNarrativeTypeMapping? mapping);

            Assert.True(mapped, $"Park-item event type {eventType} is not mapped.");
            Assert.NotNull(mapping);
        }
    }

    [Fact]
    public void TryMap_WhenEventTypeIsUnknown_ShouldNotSilentlyUseOther()
    {
        bool mapped = HistoricalNarrativeTypeMapper.TryMap(
            HistoryEntityType.Park,
            "UnexpectedValue",
            out HistoricalNarrativeTypeMapping? mapping);

        Assert.False(mapped);
        Assert.Null(mapping);
    }

    [Fact]
    public void TryMap_WhenLogoChanges_ShouldPreserveLogoBoundary()
    {
        bool mapped = HistoricalNarrativeTypeMapper.TryMap(
            HistoryEntityType.Park,
            ParkHistoryEventType.LogoChange.ToString(),
            out HistoricalNarrativeTypeMapping? mapping);

        Assert.True(mapped);
        Assert.Equal(HistoricalFactType.LogoChange, mapping!.FactType);
        Assert.Equal(HistoricalAttributeKind.Logo, mapping.AttributeKind);
        Assert.Equal(AttributeBoundaryMeaning.Unspecified, mapping.AttributeBoundaryMeaning);
    }

    [Theory]
    [InlineData(HistoryEntityType.Park)]
    [InlineData(HistoryEntityType.ParkItem)]
    [InlineData(HistoryEntityType.StandaloneAttraction)]
    public void TryMap_WhenSeasonOpeningCouldBeMistakenForInitialOpening_ShouldRequireManualClassification(
        HistoryEntityType entityType)
    {
        bool requiresManualClassification = HistoricalNarrativeTypeMapper.RequiresManualClassification(
            entityType,
            "SeasonOpening");
        bool mapped = HistoricalNarrativeTypeMapper.TryMap(
            entityType,
            "SeasonOpening",
            out HistoricalNarrativeTypeMapping? mapping);

        Assert.True(requiresManualClassification);
        Assert.False(mapped);
        Assert.Null(mapping);
    }
}
