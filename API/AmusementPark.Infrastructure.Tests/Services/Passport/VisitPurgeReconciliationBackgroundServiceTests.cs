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

public sealed class VisitPurgeReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldIdempotentlyScheduleAndMarkMissingPurgeWork()
    {
        VisitDeletionPurgeCandidate candidate = new VisitDeletionPurgeCandidate(
            VisitId.Parse("visit-1"),
            "owner-1",
            4,
            DateTime.UtcNow.AddDays(1));
        Mock<IVisitDeletionStore> deletionStore =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletionStore.Setup(store => store.ListPendingPurgeSchedulingAsync(
                VisitPurgeReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        deletionStore.Setup(store => store.MarkPurgeJobEnsuredAsync(
                candidate.VisitId,
                candidate.UserId,
                candidate.DeletionVersion,
                It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc),
                CancellationToken.None))
            .ReturnsAsync(true);
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
        services.AddScoped(_ => new VisitPurgeScheduler(jobs.Object));
        using ServiceProvider provider = services.BuildServiceProvider();
        VisitPurgeReconciliationBackgroundService service =
            new VisitPurgeReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<VisitPurgeReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        deletionStore.VerifyAll();
        jobs.VerifyAll();
    }
}
