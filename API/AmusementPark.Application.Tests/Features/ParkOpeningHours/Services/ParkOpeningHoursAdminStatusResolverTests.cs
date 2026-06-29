using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursAdminStatusResolverTests
{
    private static readonly DateTime ReferenceUtcNow = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ResolveCoverage_WhenNoScheduleData_ShouldReturnNotConfiguredWithoutCoverageDays()
    {
        ParkOpeningHoursAdminStatusResolver resolver = new ParkOpeningHoursAdminStatusResolver();

        ParkOpeningHoursAdminCoverage coverage = resolver.ResolveCoverage(null, ReferenceUtcNow);

        Assert.Equal(ParkOpeningHoursAdminStatus.NotConfigured, coverage.Status);
        Assert.Null(coverage.CompleteForDays);
        Assert.Equal(ParkOpeningHoursAdminStatusResolver.NeedsUpdateWithinDays, coverage.WarningThresholdDays);
    }

    [Fact]
    public void ResolveCoverage_WhenCoverageIsMoreThanThirtyDays_ShouldReturnUpToDate()
    {
        ParkOpeningHoursAdminStatusResolver resolver = new ParkOpeningHoursAdminStatusResolver();
        ParkOpeningHoursScheduleSummary summary = CreateSummary(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 30));

        ParkOpeningHoursAdminCoverage coverage = resolver.ResolveCoverage(summary, ReferenceUtcNow);

        Assert.Equal(ParkOpeningHoursAdminStatus.UpToDate, coverage.Status);
        Assert.Equal(32, coverage.CompleteForDays);
        Assert.Equal(new DateOnly(2026, 7, 30), coverage.CompleteUntilDate);
    }

    [Fact]
    public void ResolveCoverage_WhenCoverageIsExactlyThirtyDays_ShouldReturnNeedsUpdate()
    {
        ParkOpeningHoursAdminStatusResolver resolver = new ParkOpeningHoursAdminStatusResolver();
        ParkOpeningHoursScheduleSummary summary = CreateSummary(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 28));

        ParkOpeningHoursAdminCoverage coverage = resolver.ResolveCoverage(summary, ReferenceUtcNow);

        Assert.Equal(ParkOpeningHoursAdminStatus.NeedsUpdate, coverage.Status);
        Assert.Equal(30, coverage.CompleteForDays);
    }

    [Fact]
    public void ResolveCoverage_WhenLastCoveredDayWasYesterday_ShouldReturnExpired()
    {
        ParkOpeningHoursAdminStatusResolver resolver = new ParkOpeningHoursAdminStatusResolver();
        ParkOpeningHoursScheduleSummary summary = CreateSummary(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 28));

        ParkOpeningHoursAdminCoverage coverage = resolver.ResolveCoverage(summary, ReferenceUtcNow);

        Assert.Equal(ParkOpeningHoursAdminStatus.Expired, coverage.Status);
        Assert.Equal(0, coverage.CompleteForDays);
    }

    [Fact]
    public void IsCoverageNotificationThresholdReached_ShouldOnlyMatchThirtyDaysAndFirstExpiredDay()
    {
        ParkOpeningHoursAdminStatusResolver resolver = new ParkOpeningHoursAdminStatusResolver();
        ParkOpeningHoursScheduleSummary thirtyDays = CreateSummary(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 28));
        ParkOpeningHoursScheduleSummary thirtyOneDays = CreateSummary(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 29));
        ParkOpeningHoursScheduleSummary expiredYesterday = CreateSummary(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 28));
        ParkOpeningHoursScheduleSummary expiredEarlier = CreateSummary(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 27));

        Assert.True(resolver.IsCoverageNotificationThresholdReached(thirtyDays, 30, ReferenceUtcNow));
        Assert.False(resolver.IsCoverageNotificationThresholdReached(thirtyOneDays, 30, ReferenceUtcNow));
        Assert.True(resolver.IsCoverageNotificationThresholdReached(expiredYesterday, 0, ReferenceUtcNow));
        Assert.False(resolver.IsCoverageNotificationThresholdReached(expiredEarlier, 0, ReferenceUtcNow));
    }

    private static ParkOpeningHoursScheduleSummary CreateSummary(DateOnly startDate, DateOnly endDate)
    {
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
            FirstDate = startDate,
            LastDate = endDate,
            HasScheduleData = true,
            UpdatedAtUtc = ReferenceUtcNow,
            CoverageSegments = new[]
            {
                new ParkOpeningHoursCoverageSegmentSummary
                {
                    StartDate = startDate,
                    EndDate = endDate,
                },
            },
        };
    }
}
