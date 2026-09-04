using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Services.Passport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class VisitDeletionReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldRecoverEveryMissingDeletionSideEffect()
    {
        DateTime deletedAtUtc = DateTime.UtcNow.AddHours(-1);
        VisitDeletionReconciliationCandidate candidate = new VisitDeletionReconciliationCandidate(
            VisitId.Parse("visit-1"),
            "owner-1",
            4,
            deletedAtUtc,
            DateTime.UtcNow.AddDays(1),
            false,
            false);
        Mock<IVisitDeletionStore> deletionStore =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletionStore.Setup(store => store.ListPendingDeletionReconciliationAsync(
                VisitDeletionReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        deletionStore.Setup(store => store.MarkExportInvalidationEnsuredAsync(
                candidate.VisitId,
                candidate.UserId,
                candidate.DeletionVersion,
                It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc),
                CancellationToken.None))
            .ReturnsAsync(true);
        deletionStore.Setup(store => store.MarkPurgeJobEnsuredAsync(
                candidate.VisitId,
                candidate.UserId,
                candidate.DeletionVersion,
                It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.InvalidateOwnedAsync(
                candidate.UserId,
                deletedAtUtc,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == VisitPurgeJob.Kind
                    && request.IdempotencyKey == "passport-visit-purge:visit-1:4:0"
                    && request.Delay > TimeSpan.FromHours(23)
                    && request.Delay <= TimeSpan.FromDays(1)),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => deletionStore.Object);
        services.AddScoped(_ => exports.Object);
        services.AddScoped(_ => new VisitPurgeScheduler(jobs.Object));
        using ServiceProvider provider = services.BuildServiceProvider();
        VisitDeletionReconciliationBackgroundService service =
            new VisitDeletionReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<VisitDeletionReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        deletionStore.VerifyAll();
        exports.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenOnlyExportInvalidationIsMissing_ShouldNotReschedulePurge()
    {
        DateTime deletedAtUtc = DateTime.UtcNow.AddHours(-1);
        VisitDeletionReconciliationCandidate candidate = new VisitDeletionReconciliationCandidate(
            VisitId.Parse("visit-1"),
            "owner-1",
            4,
            deletedAtUtc,
            DateTime.UtcNow.AddDays(1),
            false,
            true);
        Mock<IVisitDeletionStore> deletionStore =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletionStore.Setup(store => store.ListPendingDeletionReconciliationAsync(
                VisitDeletionReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        deletionStore.Setup(store => store.MarkExportInvalidationEnsuredAsync(
                candidate.VisitId,
                candidate.UserId,
                candidate.DeletionVersion,
                It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.InvalidateOwnedAsync(
                candidate.UserId,
                deletedAtUtc,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => deletionStore.Object);
        services.AddScoped(_ => exports.Object);
        services.AddScoped(_ => new VisitPurgeScheduler(jobs.Object));
        using ServiceProvider provider = services.BuildServiceProvider();
        VisitDeletionReconciliationBackgroundService service =
            new VisitDeletionReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<VisitDeletionReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        deletionStore.VerifyAll();
        exports.VerifyAll();
        jobs.VerifyNoOtherCalls();
    }
}
