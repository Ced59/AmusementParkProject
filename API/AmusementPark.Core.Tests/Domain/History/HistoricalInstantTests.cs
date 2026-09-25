using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalInstantTests
{
    [Fact]
    public void FactoryMethods_ShouldKeepDistinctCacheKeysForEveryPrecision()
    {
        HistoricalInstant year = HistoricalInstant.ForYear(1998);
        HistoricalInstant month = HistoricalInstant.ForMonth(1998, 5);
        HistoricalInstant day = HistoricalInstant.ForDay(1998, 5, 12);

        Assert.Equal("Y:1998", year.CacheKey);
        Assert.Equal("M:1998-05", month.CacheKey);
        Assert.Equal("D:1998-05-12", day.CacheKey);
        Assert.Equal("1998", year.ToString());
        Assert.Equal("1998-05", month.ToString());
        Assert.Equal("1998-05-12", day.ToString());
    }

    [Fact]
    public void ForYear_ShouldRepresentWholeRequestedYearWithoutUncertainty()
    {
        HistoricalInstant instant = HistoricalInstant.ForYear(1998);
        HistoricalDateEnvelope envelope = instant.GetEnvelope();

        Assert.Equal(new DateOnly(1998, 1, 1), envelope.EarliestPossibleDate);
        Assert.Equal(new DateOnly(1998, 12, 31), envelope.LatestPossibleDate);
        Assert.False(envelope.HasUncertainPosition);
    }

    [Fact]
    public void Constructor_WhenDayPrecisionMissesDay_ShouldReuseDateInvariant()
    {
        HistoricalTemporalValidationException exception = Assert.Throws<HistoricalTemporalValidationException>(
            () => new HistoricalInstant(
                1998,
                5,
                null,
                HistoryDatePrecision.Day));

        Assert.Equal(HistoricalTemporalErrorCodes.DayRequired, exception.ErrorCode);
    }
}
