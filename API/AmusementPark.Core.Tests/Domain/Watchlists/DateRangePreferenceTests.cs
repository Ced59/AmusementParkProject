using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class DateRangePreferenceTests
{
    [Fact]
    public void Constructor_WithoutBoundary_ShouldRejectPeriod()
    {
        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => new DateRangePreference(null, null));

        Assert.Equal(UserCollectionErrorCodes.InvalidPreferredPeriod, exception.Code);
    }

    [Fact]
    public void Constructor_WhenEndPredatesStart_ShouldRejectPeriod()
    {
        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => new DateRangePreference(
                new DateOnly(2027, 6, 2),
                new DateOnly(2027, 6, 1)));

        Assert.Equal(UserCollectionErrorCodes.InvalidPreferredPeriod, exception.Code);
    }

    [Theory]
    [InlineData(2027, 4, 30, false)]
    [InlineData(2027, 5, 1, true)]
    [InlineData(2027, 5, 15, true)]
    [InlineData(2027, 5, 31, true)]
    [InlineData(2027, 6, 1, false)]
    public void Contains_WithClosedPeriod_ShouldIncludeBothBoundaries(
        int year,
        int month,
        int day,
        bool expected)
    {
        DateRangePreference period = new DateRangePreference(
            new DateOnly(2027, 5, 1),
            new DateOnly(2027, 5, 31));

        bool result = period.Contains(new DateOnly(year, month, day));

        Assert.Equal(expected, result);
    }
}
