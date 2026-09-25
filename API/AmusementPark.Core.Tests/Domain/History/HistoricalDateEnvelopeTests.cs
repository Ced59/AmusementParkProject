using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalDateEnvelopeTests
{
    [Fact]
    public void Contains_ShouldKeepClosedBoundariesInclusive()
    {
        HistoricalDateEnvelope envelope = new HistoricalDateEnvelope(
            new DateOnly(1998, 5, 1),
            new DateOnly(1998, 5, 31),
            false);

        Assert.True(envelope.Contains(new DateOnly(1998, 5, 1)));
        Assert.True(envelope.Contains(new DateOnly(1998, 5, 31)));
        Assert.False(envelope.Contains(new DateOnly(1998, 6, 1)));
    }

    [Fact]
    public void Overlaps_WhenRangesShareOneDay_ShouldReturnTrue()
    {
        HistoricalDateEnvelope first = new HistoricalDateEnvelope(
            new DateOnly(1998, 5, 1),
            new DateOnly(1998, 5, 12),
            false);
        HistoricalDateEnvelope second = new HistoricalDateEnvelope(
            new DateOnly(1998, 5, 12),
            new DateOnly(1998, 6, 1),
            false);

        Assert.True(first.Overlaps(second));
        Assert.False(first.IsDefinitelyBefore(second));
    }

    [Fact]
    public void Overlaps_WhenOneRangeIsOpen_ShouldUseKnownBoundaryOnly()
    {
        HistoricalDateEnvelope before = new HistoricalDateEnvelope(
            null,
            new DateOnly(1997, 12, 31),
            true);
        HistoricalDateEnvelope during = new HistoricalDateEnvelope(
            new DateOnly(1997, 1, 1),
            new DateOnly(1998, 12, 31),
            true);
        HistoricalDateEnvelope after = new HistoricalDateEnvelope(
            new DateOnly(1998, 1, 1),
            null,
            true);

        Assert.True(before.Overlaps(during));
        Assert.True(during.Overlaps(after));
        Assert.True(before.IsDefinitelyBefore(after));
    }

    [Fact]
    public void Constructor_WhenEndIsBeforeStart_ShouldRejectEnvelope()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalDateEnvelope(
                new DateOnly(1998, 6, 1),
                new DateOnly(1998, 5, 31),
                false));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidEnvelope, exception.ErrorCode);
    }
}
