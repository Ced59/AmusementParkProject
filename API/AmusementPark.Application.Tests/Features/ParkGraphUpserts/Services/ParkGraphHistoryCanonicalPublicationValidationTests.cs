using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphHistoryCanonicalPublicationValidationTests
{
    [Fact]
    public void ValidateCanonicalPublication_WhenVisibleEventHasNoValidSource_ShouldRejectPreview()
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            EntityType = HistoryEntityType.Park,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsVisible = true,
            Sources = new List<HistorySourceReference>
            {
                new HistorySourceReference { Url = "not-an-http-url" },
            },
        };
        ParkGraphUpsertResult result = new ParkGraphUpsertResult();

        bool isValid = ParkGraphUpsertProcessorHistoryExtensions.ValidateCanonicalPublication(
            historyEvent,
            "opening-2001",
            result);

        Assert.False(isValid);
        Assert.Contains(
            result.Errors,
            static error => error.Contains("source HTTP ou HTTPS valide", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateCanonicalPublication_WhenHiddenEventHasNoSource_ShouldAllowDraft()
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            EntityType = HistoryEntityType.Park,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsVisible = false,
        };
        ParkGraphUpsertResult result = new ParkGraphUpsertResult();

        bool isValid = ParkGraphUpsertProcessorHistoryExtensions.ValidateCanonicalPublication(
            historyEvent,
            "opening-2001",
            result);

        Assert.True(isValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCanonicalPublication_WhenRelocationHasDestination_ShouldAllowPreview()
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            EntityType = HistoryEntityType.ParkItem,
            EventType = ParkItemHistoryEventType.RelocationArrival.ToString(),
            LocationLabel = "Phantasialand",
            IsVisible = true,
            Sources = new List<HistorySourceReference>
            {
                new HistorySourceReference { Url = "https://example.com/relocation" },
            },
        };
        ParkGraphUpsertResult result = new ParkGraphUpsertResult();

        bool isValid = ParkGraphUpsertProcessorHistoryExtensions.ValidateCanonicalPublication(
            historyEvent,
            "relocation-2001",
            result);

        Assert.True(isValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void IsValidHistoryDate_WhenDayDoesNotExist_ShouldRejectDate()
    {
        bool isValid = ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryDate(
            2026,
            2,
            31,
            HistoryDatePrecision.Day);

        Assert.False(isValid);
    }
}
