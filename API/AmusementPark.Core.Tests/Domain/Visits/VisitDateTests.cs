using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class VisitDateTests
{
    [Fact]
    public void ForYear_ShouldPreserveYearPrecisionWithoutInventingMonthOrDay()
    {
        VisitDate date = VisitDate.ForYear(1998, true);

        Assert.Equal(1998, date.Year);
        Assert.Null(date.Month);
        Assert.Null(date.Day);
        Assert.Equal(VisitDatePrecision.Year, date.Precision);
        Assert.True(date.IsApproximate);
        Assert.Equal("~1998", date.ToString());
    }

    [Fact]
    public void ForMonth_ShouldPreserveMonthPrecisionWithoutInventingDay()
    {
        VisitDate date = VisitDate.ForMonth(2024, 7);

        Assert.Equal(7, date.Month);
        Assert.Null(date.Day);
        Assert.Equal(VisitDatePrecision.Month, date.Precision);
        Assert.Equal("2024-07", date.ToString());
    }

    [Fact]
    public void ForDay_WhenLeapDayExists_ShouldCreateExactDate()
    {
        VisitDate date = VisitDate.ForDay(2024, 2, 29);

        Assert.Equal(29, date.Day);
        Assert.Equal(VisitDatePrecision.Day, date.Precision);
        Assert.Equal("2024-02-29", date.ToString());
    }

    [Theory]
    [InlineData(2023, 2, 29)]
    [InlineData(2024, 4, 31)]
    [InlineData(2024, 1, 0)]
    public void ForDay_WhenCalendarDayDoesNotExist_ShouldRejectIt(int year, int month, int day)
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => VisitDate.ForDay(year, month, day));

        Assert.Equal(VisitDateErrorCodes.InvalidDay, exception.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void ForYear_WhenYearIsOutsideCalendarRange_ShouldRejectIt(int year)
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => VisitDate.ForYear(year));

        Assert.Equal(VisitDateErrorCodes.InvalidYear, exception.ErrorCode);
    }

    [Fact]
    public void ForDay_WhenDatePredates1900_ShouldPreserveIt()
    {
        VisitDate date = VisitDate.ForDay(1843, 7, 15);

        Assert.Equal(new DateOnly(1843, 7, 15), date.GetEarliestPossibleDate());
        Assert.Equal("1843-07-15", date.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ForMonth_WhenMonthIsOutsideCalendarRange_ShouldRejectIt(int month)
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => VisitDate.ForMonth(2024, month));

        Assert.Equal(VisitDateErrorCodes.InvalidMonth, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenYearPrecisionContainsMonth_ShouldRejectIncoherentPrecision()
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => new VisitDate(2024, 2, null, VisitDatePrecision.Year, false));

        Assert.Equal(VisitDateErrorCodes.MonthForbidden, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenMonthPrecisionContainsDay_ShouldRejectIncoherentPrecision()
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => new VisitDate(2024, 2, 1, VisitDatePrecision.Month, false));

        Assert.Equal(VisitDateErrorCodes.DayForbidden, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenDayPrecisionMissesComponents_ShouldRejectIncoherentPrecision()
    {
        VisitDateValidationException missingMonth = Assert.Throws<VisitDateValidationException>(
            () => new VisitDate(2024, null, null, VisitDatePrecision.Day, false));
        VisitDateValidationException missingDay = Assert.Throws<VisitDateValidationException>(
            () => new VisitDate(2024, 2, null, VisitDatePrecision.Day, false));

        Assert.Equal(VisitDateErrorCodes.MonthRequired, missingMonth.ErrorCode);
        Assert.Equal(VisitDateErrorCodes.DayRequired, missingDay.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPrecisionIsUnknown_ShouldRejectIt()
    {
        VisitDateValidationException exception = Assert.Throws<VisitDateValidationException>(
            () => new VisitDate(2024, null, null, (VisitDatePrecision)99, false));

        Assert.Equal(VisitDateErrorCodes.InvalidPrecision, exception.ErrorCode);
    }

    [Fact]
    public void Bounds_ShouldDescribePossiblePeriodWithoutChangingPrecision()
    {
        VisitDate year = VisitDate.ForYear(2024);
        VisitDate month = VisitDate.ForMonth(2024, 2);
        VisitDate day = VisitDate.ForDay(2024, 9, 3);

        Assert.Equal(new DateOnly(2024, 1, 1), year.GetEarliestPossibleDate());
        Assert.Equal(new DateOnly(2024, 12, 31), year.GetLatestPossibleDate());
        Assert.Equal(new DateOnly(2024, 2, 1), month.GetEarliestPossibleDate());
        Assert.Equal(new DateOnly(2024, 2, 29), month.GetLatestPossibleDate());
        Assert.Equal(new DateOnly(2024, 9, 3), day.GetEarliestPossibleDate());
        Assert.Equal(new DateOnly(2024, 9, 3), day.GetLatestPossibleDate());
        Assert.Equal(VisitDatePrecision.Year, year.Precision);
        Assert.Null(year.Month);
        Assert.Null(year.Day);
    }

    [Fact]
    public void ValueEquality_ShouldIncludePrecisionAndApproximation()
    {
        VisitDate exact = VisitDate.ForMonth(2024, 7);
        VisitDate same = VisitDate.ForMonth(2024, 7);
        VisitDate approximate = VisitDate.ForMonth(2024, 7, true);

        Assert.Equal(exact, same);
        Assert.NotEqual(exact, approximate);
    }

    [Fact]
    public void ServiceDayConvention_ShouldRemainAnExplicitDomainChoice()
    {
        Assert.NotEqual(
            LocalServiceDayConvention.VisitStartLocalDate,
            LocalServiceDayConvention.UserSelectedServiceDate);
    }
}
