using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalPeriodTests
{
    [Fact]
    public void Point_ShouldRepeatSameStructuredDate()
    {
        HistoricalDate date = HistoricalDate.ForMonth(1998, 5);

        HistoricalPeriod period = HistoricalPeriod.Point(date);

        Assert.Same(date, period.Start);
        Assert.Same(date, period.End);
        Assert.True(period.IsPoint);
        Assert.Equal(HistoricalPeriodOrdering.Point, period.Ordering);
    }

    [Fact]
    public void Constructor_WhenBothBoundariesAreOpen_ShouldRejectPeriod()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalPeriod(
                null,
                null,
                PeriodBoundaryConfidence.Confirmed,
                PeriodBoundaryConfidence.Confirmed));

        Assert.Equal(HistoricalTemporalErrorCodes.PeriodRequiresBoundary, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenEndIsCertainlyBeforeStart_ShouldRejectPeriod()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalPeriod(
                HistoricalDate.ForYear(2000),
                HistoricalDate.ForYear(1998),
                PeriodBoundaryConfidence.Confirmed,
                PeriodBoundaryConfidence.Confirmed));

        Assert.Equal(HistoricalTemporalErrorCodes.PeriodEndBeforeStart, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPartialBoundariesCanOverlap_ShouldKeepAmbiguity()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForYear(1998),
            HistoricalDate.ForMonth(1998, 5),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        Assert.Equal(HistoricalPeriodOrdering.Ambiguous, period.Ordering);
        Assert.True(period.HasUncertainBoundary);
    }

    [Fact]
    public void From_ShouldCreateOpenEndWithoutSentinelDate()
    {
        HistoricalPeriod period = HistoricalPeriod.From(HistoricalDate.ForDay(1998, 5, 12));
        HistoricalDateEnvelope envelope = period.GetPossibleEnvelope();

        Assert.False(period.HasOpenStart);
        Assert.True(period.HasOpenEnd);
        Assert.Equal(new DateOnly(1998, 5, 12), envelope.EarliestPossibleDate);
        Assert.Null(envelope.LatestPossibleDate);
    }

    [Fact]
    public void Until_ShouldCreateOpenStartWithoutSentinelDate()
    {
        HistoricalPeriod period = HistoricalPeriod.Until(HistoricalDate.ForDay(2004, 8, 1));
        HistoricalDateEnvelope envelope = period.GetPossibleEnvelope();

        Assert.True(period.HasOpenStart);
        Assert.False(period.HasOpenEnd);
        Assert.Null(envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(2004, 8, 1), envelope.LatestPossibleDate);
    }

    [Fact]
    public void Match_WhenRequestedYearIsStrictlyInsideConfirmedPartialBoundaries_ShouldContainItEntirely()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForYear(1998),
            HistoricalDate.ForYear(2000),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForYear(1999));

        Assert.Equal(HistoricalPeriodMatch.EntirelyContained, match);
    }

    [Fact]
    public void Match_WhenConfirmedPointFallsInsideRequestedYear_ShouldReturnDefinitePartialOverlap()
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForYear(1998));

        Assert.Equal(HistoricalPeriodMatch.DefinitePartialOverlap, match);
    }

    [Fact]
    public void Match_WhenConfirmedPeriodCoversOnlyPartOfRequestedYear_ShouldReturnDefinitePartialOverlap()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForDay(1998, 5, 1),
            HistoricalDate.ForDay(1998, 5, 10),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForYear(1998));

        Assert.Equal(HistoricalPeriodMatch.DefinitePartialOverlap, match);
    }

    [Fact]
    public void Match_WhenYearPrecisionPointIsQueriedByMonth_ShouldRemainPossible()
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(1998));

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForMonth(1998, 5));

        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, match);
    }

    [Fact]
    public void Match_WhenRequestedDayEqualsClosedBoundary_ShouldContainItEntirely()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForDay(1998, 5, 1),
            HistoricalDate.ForDay(1998, 5, 10),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        Assert.Equal(
            HistoricalPeriodMatch.EntirelyContained,
            period.Match(HistoricalInstant.ForDay(1998, 5, 1)));
        Assert.Equal(
            HistoricalPeriodMatch.EntirelyContained,
            period.Match(HistoricalInstant.ForDay(1998, 5, 10)));
    }

    [Fact]
    public void Match_WhenRequestedDateIsOutsidePossibleEnvelope_ShouldReturnOutside()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForYear(1998),
            HistoricalDate.ForYear(2000),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForYear(2002));

        Assert.Equal(HistoricalPeriodMatch.Outside, match);
    }

    [Fact]
    public void Match_WhenBoundaryIsEstimated_ShouldNotClaimEntireContainment()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForDay(1998, 1, 1),
            HistoricalDate.ForDay(2000, 12, 31),
            PeriodBoundaryConfidence.Estimated,
            PeriodBoundaryConfidence.Confirmed);

        HistoricalPeriodMatch match = period.Match(HistoricalInstant.ForDay(1999, 6, 1));

        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, match);
    }

    [Fact]
    public void Match_WhenApproximateStartTouchesRequestedDay_ShouldReturnPossibleOverlap()
    {
        HistoricalPeriod period = HistoricalPeriod.From(
            HistoricalDate.ForDay(1998, 5, 12, isApproximate: true));

        HistoricalPeriodMatch boundaryMatch = period.Match(
            HistoricalInstant.ForDay(1998, 5, 12));
        HistoricalPeriodMatch followingDayMatch = period.Match(
            HistoricalInstant.ForDay(1998, 5, 13));

        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, boundaryMatch);
        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, followingDayMatch);
    }

    [Fact]
    public void Match_WhenApproximateEndTouchesRequestedDay_ShouldReturnPossibleOverlap()
    {
        HistoricalPeriod period = HistoricalPeriod.Until(
            HistoricalDate.ForDay(2004, 8, 1, isApproximate: true));

        HistoricalPeriodMatch boundaryMatch = period.Match(
            HistoricalInstant.ForDay(2004, 8, 1));
        HistoricalPeriodMatch precedingDayMatch = period.Match(
            HistoricalInstant.ForDay(2004, 7, 31));

        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, boundaryMatch);
        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, precedingDayMatch);
    }

    [Fact]
    public void Match_WhenEndIsApproximate_ShouldPreserveCertaintyFromExactStart()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForDay(1998, 1, 1),
            HistoricalDate.ForDay(1998, 12, 31, isApproximate: true),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        Assert.Equal(
            HistoricalPeriodMatch.EntirelyContained,
            period.Match(HistoricalInstant.ForDay(1998, 1, 1)));
        Assert.Equal(
            HistoricalPeriodMatch.DefinitePartialOverlap,
            period.Match(HistoricalInstant.ForYear(1998)));
    }

    [Fact]
    public void Match_WhenStartIsApproximate_ShouldPreserveCertaintyFromExactEnd()
    {
        HistoricalPeriod period = new HistoricalPeriod(
            HistoricalDate.ForDay(1998, 1, 1, isApproximate: true),
            HistoricalDate.ForDay(1998, 12, 31),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);

        Assert.Equal(
            HistoricalPeriodMatch.EntirelyContained,
            period.Match(HistoricalInstant.ForDay(1998, 12, 31)));
        Assert.Equal(
            HistoricalPeriodMatch.DefinitePartialOverlap,
            period.Match(HistoricalInstant.ForYear(1998)));
    }

    [Fact]
    public void Match_WhenStartQualifierIsUncertain_ShouldNotClaimItsEnvelopeBoundary()
    {
        HistoricalPeriod period = HistoricalPeriod.From(
            HistoricalDate.ForYear(1998, qualifier: DateQualifier.Early));

        HistoricalPeriodMatch match = period.Match(
            HistoricalInstant.ForDay(1998, 12, 31));

        Assert.Equal(HistoricalPeriodMatch.PossibleOverlap, match);
    }

    [Fact]
    public void Match_WhenPointEndIsConfirmed_ShouldUseItIndependentlyFromEstimatedStart()
    {
        HistoricalDate date = HistoricalDate.ForDay(1998, 5, 12);
        HistoricalPeriod period = new HistoricalPeriod(
            date,
            date,
            PeriodBoundaryConfidence.Estimated,
            PeriodBoundaryConfidence.Confirmed);

        HistoricalPeriodMatch match = period.Match(
            HistoricalInstant.ForDay(1998, 5, 12));

        Assert.Equal(HistoricalPeriodMatch.EntirelyContained, match);
    }

    [Fact]
    public void Constructor_WhenConfidenceIsUnknown_ShouldRejectIt()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalPeriod.Point(
                HistoricalDate.ForYear(1998),
                (PeriodBoundaryConfidence)99));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidBoundaryConfidence, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenOpenBoundaryHasNonNeutralConfidence_ShouldRejectIt()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalPeriod(
                null,
                HistoricalDate.ForYear(1998),
                PeriodBoundaryConfidence.Estimated,
                PeriodBoundaryConfidence.Confirmed));

        Assert.Equal(
            HistoricalTemporalErrorCodes.OpenBoundaryConfidenceMustBeConfirmed,
            exception.ErrorCode);
    }
}
