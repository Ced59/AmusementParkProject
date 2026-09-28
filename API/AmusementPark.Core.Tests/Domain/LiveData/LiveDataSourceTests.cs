using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveDataSourceTests
{
    [Fact]
    public void Constructor_WithActiveSource_ShouldExposePollingContract()
    {
        LiveDataSource source = CreateSource(LiveDataSourceStatus.Active);

        Assert.Equal("Pilot source", source.DisplayName);
        Assert.Equal(LiveDataSourceType.AuthorizedAggregator, source.Type);
        Assert.Equal(TimeSpan.FromMinutes(5), source.MinimumPollingInterval);
        Assert.Equal(TimeSpan.FromMinutes(15), source.DefaultTtl);
        Assert.True(source.CanPoll);
    }

    [Theory]
    [InlineData(LiveDataSourceStatus.Candidate)]
    [InlineData(LiveDataSourceStatus.Suspended)]
    [InlineData(LiveDataSourceStatus.Retired)]
    public void CanPoll_WhenSourceIsNotActive_ShouldReturnFalse(LiveDataSourceStatus status)
    {
        LiveDataSource source = CreateSource(status);

        Assert.False(source.CanPoll);
    }

    [Fact]
    public void Constructor_WhenStatusIsUnknown_ShouldRejectSource()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreateSource((LiveDataSourceStatus)999));

        Assert.Equal(LiveDataErrorCodes.InvalidEnum, exception.Code);
    }

    [Fact]
    public void Constructor_WhenTtlDoesNotCoverPollingInterval_ShouldRejectSource()
    {
        SourceUsagePolicy policy = CreateUsagePolicy();

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new LiveDataSource(
                LiveDataSourceId.Parse("pilot"),
                LiveDataSourceType.AuthorizedAggregator,
                "Pilot source",
                policy,
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(5),
                LiveDataSourceStatus.Active));

        Assert.Equal(LiveDataErrorCodes.InvalidDuration, exception.Code);
    }

    private static LiveDataSource CreateSource(LiveDataSourceStatus status)
    {
        return new LiveDataSource(
            LiveDataSourceId.Parse("pilot"),
            LiveDataSourceType.AuthorizedAggregator,
            "  Pilot source  ",
            CreateUsagePolicy(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            status);
    }

    private static SourceUsagePolicy CreateUsagePolicy()
    {
        return new SourceUsagePolicy(
            "terms-1",
            "https://example.org/terms",
            true,
            true,
            false,
            true,
            "live.attribution.example",
            new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc));
    }
}
