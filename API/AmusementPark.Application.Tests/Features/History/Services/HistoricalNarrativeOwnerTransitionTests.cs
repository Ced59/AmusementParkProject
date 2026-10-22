using System.Text.Json;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeOwnerTransitionTests
{
    [Theory]
    [InlineData("OwnershipChange")]
    [InlineData("Acquisition")]
    [InlineData("Sale")]
    public void BuildStructuredValue_ShouldUseExplicitOwnerNames(string eventType)
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            EntityType = HistoryEntityType.Park,
            EventType = eventType,
            PreviousOwnerName = "  Founding family  ",
            NewOwnerName = "  Purchasing group  ",
            NewOperatorId = "different-operator",
            NewName = "Unchanged park name",
        };
        Assert.True(HistoricalNarrativeTypeMapper.TryMap(HistoryEntityType.Park, eventType, out HistoricalNarrativeTypeMapping? mapping));

        string? value = HistoricalNarrativeCanonicalFactFactory.BuildStructuredValue(historyEvent, mapping!);

        Assert.NotNull(value);
        using JsonDocument document = JsonDocument.Parse(value);
        Assert.Equal("Founding family", document.RootElement.GetProperty("previous").GetString());
        Assert.Equal("Purchasing group", document.RootElement.GetProperty("next").GetString());
        Assert.False(document.RootElement.TryGetProperty("nextId", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void BuildStructuredValue_WhenOwnerIsMissing_ShouldNotInferFromOperatorOrParkName(string? ownerName)
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            PreviousOwnerName = "Founding family",
            NewOwnerName = ownerName,
            NewOperatorId = "operator-1",
            NewName = "Park name",
        };
        Assert.True(HistoricalNarrativeTypeMapper.TryMap(HistoryEntityType.Park, "Acquisition", out HistoricalNarrativeTypeMapping? mapping));

        Assert.Null(HistoricalNarrativeCanonicalFactFactory.BuildStructuredValue(historyEvent, mapping!));
    }

    [Fact]
    public void BuildStructuredValue_WhenPreviousOwnerIsUnknown_ShouldKeepOnlyNewOwner()
    {
        HistoryEvent historyEvent = new HistoryEvent { NewOwnerName = "Purchasing group" };
        Assert.True(HistoricalNarrativeTypeMapper.TryMap(HistoryEntityType.Park, "Acquisition", out HistoricalNarrativeTypeMapping? mapping));

        using JsonDocument document = JsonDocument.Parse(HistoricalNarrativeCanonicalFactFactory.BuildStructuredValue(historyEvent, mapping!)!);

        Assert.Equal("Purchasing group", document.RootElement.GetProperty("next").GetString());
        Assert.False(document.RootElement.TryGetProperty("previous", out _));
    }
}
