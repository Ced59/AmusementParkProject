using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalDateTests
{
    [Fact]
    public void ForYear_ShouldPreservePrecisionAndReturnWholeCivilYear()
    {
        HistoricalDate date = HistoricalDate.ForYear(1998);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Null(date.Month);
        Assert.Null(date.Day);
        Assert.Equal(HistoryDatePrecision.Year, date.Precision);
        Assert.Equal(new DateOnly(1998, 1, 1), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(1998, 12, 31), envelope.LatestPossibleDate);
        Assert.True(date.HasUncertainBoundary);
        Assert.Equal("1998", date.ToString());
    }

    [Fact]
    public void ForMonth_ShouldUseRealLeapYearBoundaryWithoutInventingDay()
    {
        HistoricalDate date = HistoricalDate.ForMonth(2024, 2);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Null(date.Day);
        Assert.Equal(new DateOnly(2024, 2, 1), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(2024, 2, 29), envelope.LatestPossibleDate);
        Assert.Equal("2024-02", date.ToString());
    }

    [Fact]
    public void ForDay_ShouldCreateExactCivilEnvelope()
    {
        HistoricalDate date = HistoricalDate.ForDay(1998, 5, 12);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.True(envelope.IsExactDay);
        Assert.False(date.HasUncertainBoundary);
        Assert.Equal(new DateOnly(1998, 5, 12), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(1998, 5, 12), envelope.LatestPossibleDate);
    }

    [Fact]
    public void BeforeYear_ShouldCreateExclusiveOpenStartEnvelope()
    {
        HistoricalDate date = HistoricalDate.ForYear(1998, qualifier: DateQualifier.Before);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Null(envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(1997, 12, 31), envelope.LatestPossibleDate);
        Assert.False(date.HasUncertainBoundary);
        Assert.Equal("before:1998", date.ToString());
    }

    [Fact]
    public void AfterMonth_ShouldCreateExclusiveOpenEndEnvelope()
    {
        HistoricalDate date = HistoricalDate.ForMonth(1998, 5, qualifier: DateQualifier.After);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Equal(new DateOnly(1998, 6, 1), envelope.EarliestPossibleDate);
        Assert.Null(envelope.LatestPossibleDate);
        Assert.False(date.HasUncertainBoundary);
    }

    [Theory]
    [InlineData(DateQualifier.Early)]
    [InlineData(DateQualifier.Mid)]
    [InlineData(DateQualifier.Late)]
    public void PositionalQualifier_ShouldKeepBaseEnvelopeAndMarkBoundaryUncertain(
        DateQualifier qualifier)
    {
        HistoricalDate date = HistoricalDate.ForMonth(1998, 5, qualifier: qualifier);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Equal(new DateOnly(1998, 5, 1), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(1998, 5, 31), envelope.LatestPossibleDate);
        Assert.True(date.HasUncertainBoundary);
    }

    [Fact]
    public void Circa_WhenApproximate_ShouldKeepEnvelopeWithoutInventingTolerance()
    {
        HistoricalDate date = HistoricalDate.ForYear(
            2004,
            true,
            DateQualifier.Circa);
        HistoricalDateEnvelope envelope = date.GetEnvelope();

        Assert.Equal(new DateOnly(2004, 1, 1), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(2004, 12, 31), envelope.LatestPossibleDate);
        Assert.True(envelope.HasUncertainPosition);
        Assert.Equal("circa:2004", date.ToString());
    }

    [Fact]
    public void Circa_WhenNotApproximate_ShouldRejectContradictoryDate()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForYear(2004, qualifier: DateQualifier.Circa));

        Assert.Equal(
            HistoricalTemporalErrorCodes.CircaRequiresApproximation,
            exception.ErrorCode);
    }

    [Theory]
    [InlineData(2023, 2, 29)]
    [InlineData(2024, 4, 31)]
    [InlineData(2024, 1, 0)]
    public void ForDay_WhenDayDoesNotExist_ShouldRejectIt(int year, int month, int day)
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForDay(year, month, day));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidDay, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPrecisionAndPartsConflict_ShouldRejectDate()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalDate(
                1998,
                5,
                null,
                HistoryDatePrecision.Year,
                false,
                null));

        Assert.Equal(HistoricalTemporalErrorCodes.MonthForbidden, exception.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void ForYear_WhenYearIsOutsideCalendar_ShouldRejectIt(int year)
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForYear(year));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidYear, exception.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ForMonth_WhenMonthIsOutsideCalendar_ShouldRejectIt(int month)
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForMonth(1998, month));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidMonth, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPrecisionIsUnknown_ShouldRejectDate()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalDate(
                1998,
                null,
                null,
                (HistoryDatePrecision)99,
                false,
                null));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidPrecision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenQualifierIsUnknown_ShouldRejectDate()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalDate(
                1998,
                null,
                null,
                HistoryDatePrecision.Year,
                false,
                (DateQualifier)99));

        Assert.Equal(HistoricalTemporalErrorCodes.InvalidQualifier, exception.ErrorCode);
    }

    [Fact]
    public void BeforeMinimumCalendarDate_ShouldRejectEmptyRange()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForYear(1, qualifier: DateQualifier.Before));

        Assert.Equal(HistoricalTemporalErrorCodes.EmptyQualifiedRange, exception.ErrorCode);
    }

    [Fact]
    public void AfterMaximumCalendarDate_ShouldRejectEmptyRange()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => HistoricalDate.ForYear(9999, qualifier: DateQualifier.After));

        Assert.Equal(HistoricalTemporalErrorCodes.EmptyQualifiedRange, exception.ErrorCode);
    }
}
