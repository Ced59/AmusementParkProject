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
        HistoryEvent historyEvent = new HistoryEvent { IsVisible = false };
        ParkGraphUpsertResult result = new ParkGraphUpsertResult();

        bool isValid = ParkGraphUpsertProcessorHistoryExtensions.ValidateCanonicalPublication(
            historyEvent,
            "opening-2001",
            result);

        Assert.True(isValid);
        Assert.Empty(result.Errors);
    }
}
