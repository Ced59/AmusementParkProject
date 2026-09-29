using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveHistoryRetentionPolicyTests
{
    [Fact]
    public void GetBucketStartUtc_ShouldFloorTimestampToConfiguredUtcBucket()
    {
        LiveHistoryRetentionPolicy policy = new LiveHistoryRetentionPolicy(
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(400),
            TimeSpan.FromHours(1));
        DateTime observedAtUtc = new DateTime(
            2026,
            9,
            29,
            14,
            37,
            12,
            DateTimeKind.Utc).AddTicks(9);

        DateTime bucketStartUtc = policy.GetBucketStartUtc(observedAtUtc);

        Assert.Equal(
            new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc),
            bucketStartUtc);
        Assert.Equal(
            observedAtUtc.AddDays(7),
            policy.GetRawExpirationUtc(observedAtUtc));
        Assert.Equal(
            bucketStartUtc.AddHours(1).AddDays(400),
            policy.GetAggregateExpirationUtc(bucketStartUtc));
    }

    [Fact]
    public void Constructor_WhenAggregateRetentionIsShorterThanRaw_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new LiveHistoryRetentionPolicy(
                TimeSpan.FromDays(8),
                TimeSpan.FromDays(7),
                TimeSpan.FromHours(1)));

        Assert.Equal(LiveDataErrorCodes.InvalidDuration, exception.Code);
    }

    [Fact]
    public void Constructor_WhenBucketDoesNotDivideUtcDay_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new LiveHistoryRetentionPolicy(
                TimeSpan.FromDays(7),
                TimeSpan.FromDays(400),
                TimeSpan.FromHours(5)));

        Assert.Equal(LiveDataErrorCodes.InvalidDuration, exception.Code);
    }
}
