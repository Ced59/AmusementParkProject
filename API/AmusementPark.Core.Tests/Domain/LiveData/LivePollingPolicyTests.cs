using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LivePollingPolicyTests
{
    private static readonly DateTime AttemptedAtUtc =
        new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenIntervalIsUnderFiveMinutes_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreatePolicy(pollingInterval: TimeSpan.FromMinutes(4)));

        Assert.Equal(LiveDataErrorCodes.InvalidPollingPolicy, exception.Code);
    }

    [Fact]
    public void PlanNext_AfterSuccess_ShouldResetFailuresAndCloseCircuit()
    {
        LivePollingPolicy policy = CreatePolicy();

        LivePollingSchedule schedule = policy.PlanNext(
            AttemptedAtUtc,
            LivePollingAttemptOutcome.Success,
            4,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(20));

        Assert.Equal(AttemptedAtUtc.AddMinutes(5).AddSeconds(20), schedule.NextAttemptAtUtc);
        Assert.Equal(0, schedule.ConsecutiveFailures);
        Assert.Null(schedule.CircuitOpenUntilUtc);
        Assert.False(schedule.CircuitOpened);
    }

    [Fact]
    public void PlanNext_AfterRepeatedFailure_ShouldOpenCircuitAndUseCircuitDelay()
    {
        LivePollingPolicy policy = CreatePolicy(circuitThreshold: 3);

        LivePollingSchedule schedule = policy.PlanNext(
            AttemptedAtUtc,
            LivePollingAttemptOutcome.Failure,
            2,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(3, schedule.ConsecutiveFailures);
        Assert.True(schedule.CircuitOpened);
        Assert.Equal(AttemptedAtUtc.AddMinutes(30), schedule.CircuitOpenUntilUtc);
        Assert.Equal(AttemptedAtUtc.AddMinutes(30), schedule.NextAttemptAtUtc);
    }

    [Fact]
    public void PlanNext_WhenRateLimited_ShouldHonorLongerRetryAfter()
    {
        LivePollingPolicy policy = CreatePolicy();

        LivePollingSchedule schedule = policy.PlanNext(
            AttemptedAtUtc,
            LivePollingAttemptOutcome.RateLimited,
            0,
            TimeSpan.FromMinutes(20),
            TimeSpan.FromSeconds(15));

        Assert.Equal(AttemptedAtUtc.AddMinutes(20).AddSeconds(15), schedule.NextAttemptAtUtc);
        Assert.Equal(1, schedule.ConsecutiveFailures);
    }

    [Fact]
    public void PlanNext_WhenRetryAfterExceedsConfiguredMaximum_ShouldStillHonorProvider()
    {
        LivePollingPolicy policy = CreatePolicy();

        LivePollingSchedule schedule = policy.PlanNext(
            AttemptedAtUtc,
            LivePollingAttemptOutcome.RateLimited,
            0,
            TimeSpan.FromDays(60),
            TimeSpan.Zero);

        Assert.Equal(AttemptedAtUtc.AddDays(60), schedule.NextAttemptAtUtc);
    }

    [Fact]
    public void PlanNext_AfterFailures_ShouldCapExponentialBackoff()
    {
        LivePollingPolicy policy = CreatePolicy();

        LivePollingSchedule schedule = policy.PlanNext(
            AttemptedAtUtc,
            LivePollingAttemptOutcome.Failure,
            20,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(AttemptedAtUtc.AddHours(1), schedule.NextAttemptAtUtc);
    }

    private static LivePollingPolicy CreatePolicy(
        TimeSpan? pollingInterval = null,
        int circuitThreshold = 5)
    {
        return new LivePollingPolicy(
            pollingInterval ?? TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(1),
            circuitThreshold,
            TimeSpan.FromMinutes(30));
    }
}
