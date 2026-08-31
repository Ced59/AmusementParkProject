using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Infrastructure.Configuration.BackgroundJobs;
using AmusementPark.Infrastructure.Services.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.BackgroundJobs;

public sealed class DurableBackgroundJobWorkerTests
{
    [Fact]
    public void SettingsBind_WhenConfigurationIsMissing_ShouldUseVpsSafeDefaults()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        DurableBackgroundJobWorkerSettings settings = DurableBackgroundJobWorkerSettings.Bind(configuration);

        Assert.True(settings.Enabled);
        Assert.Equal(1, settings.HeavyWorkerCount);
        Assert.Equal(1, settings.LightWorkerCount);
        Assert.Equal(TimeSpan.FromMinutes(2), settings.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.LeaseRenewalInterval);
        Assert.Equal(TimeSpan.FromMilliseconds(250), settings.EmptyQueueInitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.EmptyQueueMaximumDelay);
        Assert.Equal(100, settings.LeaseRecoveryBatchSize);
        Assert.Equal(TimeSpan.FromHours(1), settings.UnknownKindGracePeriod);
    }

    [Fact]
    public void SettingsBind_WhenHeavyConcurrencyExceedsTheVpsBudget_ShouldFailFast()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>
        {
            ["DurableBackgroundJobs:Worker:HeavyWorkerCount"] = "2",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DurableBackgroundJobWorkerSettings.Bind(configuration));

        Assert.Contains("HeavyWorkerCount", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IdleBackoff_ShouldGrowToTheMaximumAndResetAfterWork()
    {
        DurableBackgroundJobIdleBackoff backoff = new DurableBackgroundJobIdleBackoff(
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromSeconds(1),
            2);

        Assert.Equal(TimeSpan.FromMilliseconds(250), backoff.TakeNextDelay());
        Assert.Equal(TimeSpan.FromMilliseconds(500), backoff.TakeNextDelay());
        Assert.Equal(TimeSpan.FromSeconds(1), backoff.TakeNextDelay());
        Assert.Equal(TimeSpan.FromSeconds(1), backoff.TakeNextDelay());

        backoff.Reset();

        Assert.Equal(TimeSpan.FromMilliseconds(250), backoff.TakeNextDelay());
    }

    [Fact]
    public async Task ClaimCoordinator_ShouldNotLeaseBeyondThePerKindConcurrencyBudget()
    {
        DurableBackgroundJobHandlerDefinition definition = CreateDefinition(maximumConcurrency: 1);
        DurableBackgroundJobClaimCoordinator coordinator =
            new DurableBackgroundJobClaimCoordinator(new[] { definition });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .SetupSequence(item => item.TryLeaseNextAsync(
                It.IsAny<LeaseBackgroundJobRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeasedJob("job-1"))
            .ReturnsAsync(CreateLeasedJob("job-2"));

        DurableBackgroundJobClaim? first = await coordinator.TryClaimAsync(
            repository.Object,
            DurableBackgroundJobWorkload.Heavy,
            "worker-1",
            TimeSpan.FromMinutes(2),
            CancellationToken.None);
        DurableBackgroundJobClaim? whileBusy = await coordinator.TryClaimAsync(
            repository.Object,
            DurableBackgroundJobWorkload.Heavy,
            "worker-2",
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(whileBusy);
        repository.Verify(
            item => item.TryLeaseNextAsync(
                It.IsAny<LeaseBackgroundJobRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        first.Dispose();
        using DurableBackgroundJobClaim? afterRelease = await coordinator.TryClaimAsync(
            repository.Object,
            DurableBackgroundJobWorkload.Heavy,
            "worker-2",
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

        Assert.Equal("job-2", afterRelease?.Job.Id);
    }

    [Fact]
    public async Task ClaimCoordinator_ShouldOnlyRequestKindsFromTheSelectedWorkload()
    {
        DurableBackgroundJobHandlerDefinition heavy = CreateDefinition(maximumConcurrency: 1);
        DurableBackgroundJobHandlerDefinition light = new DurableBackgroundJobHandlerDefinition(
            "light.kind",
            DurableBackgroundJobWorkload.Light,
            new[] { 1 },
            TimeSpan.FromMinutes(1),
            3,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMinutes(1));
        DurableBackgroundJobClaimCoordinator coordinator =
            new DurableBackgroundJobClaimCoordinator(new[] { heavy, light });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.TryLeaseNextAsync(
                It.Is<LeaseBackgroundJobRequest>(request =>
                    request.Kinds.Count == 1 && request.Kinds.Contains("heavy.kind")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DurableBackgroundJob?)null);

        DurableBackgroundJobClaim? claim = await coordinator.TryClaimAsync(
            repository.Object,
            DurableBackgroundJobWorkload.Heavy,
            "worker-1",
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

        Assert.Null(claim);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ClaimCoordinator_ShouldLeaseOnlyAgedKindsAbsentFromTheRegistry()
    {
        DurableBackgroundJobHandlerDefinition definition = CreateDefinition(maximumConcurrency: 1);
        DurableBackgroundJobClaimCoordinator coordinator =
            new DurableBackgroundJobClaimCoordinator(new[] { definition });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.TryLeaseNextUnknownKindAsync(
                It.Is<LeaseUnknownBackgroundJobRequest>(request =>
                    request.KnownKinds.Count == 1 &&
                    request.KnownKinds.Contains("heavy.kind") &&
                    request.MinimumAge == TimeSpan.FromHours(1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeasedJob("job-unknown", "obsolete.kind"));

        using DurableBackgroundJobClaim? claim = await coordinator.TryClaimUnknownKindAsync(
            repository.Object,
            "worker-unknown",
            TimeSpan.FromMinutes(2),
            TimeSpan.FromHours(1),
            CancellationToken.None);

        Assert.Equal("obsolete.kind", claim?.Job.Kind);
        repository.VerifyAll();
    }

    private static DurableBackgroundJobHandlerDefinition CreateDefinition(int maximumConcurrency)
    {
        return new DurableBackgroundJobHandlerDefinition(
            "heavy.kind",
            DurableBackgroundJobWorkload.Heavy,
            new[] { 1 },
            TimeSpan.FromMinutes(1),
            3,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMinutes(1),
            maximumConcurrency);
    }

    private static DurableBackgroundJob CreateLeasedJob(string id, string kind = "heavy.kind")
    {
        using JsonDocument payload = JsonDocument.Parse("{}");
        DateTime nowUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        return new DurableBackgroundJob(
            id,
            kind,
            id,
            null,
            1,
            payload.RootElement.Clone(),
            1,
            null,
            DurableBackgroundJobStatus.Leased,
            0,
            1,
            nowUtc,
            "worker-1",
            "lease-token",
            nowUtc.AddMinutes(2),
            nowUtc,
            nowUtc,
            null,
            null,
            null);
    }
}
