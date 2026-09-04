using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class VisitDeletionHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Preview_ReturnsTheCurrentVersionAndBoundedImpact()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetImpactAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionImpact(4, 3));
        GetVisitDeletionPreviewQueryHandler handler =
            new GetVisitDeletionPreviewQueryHandler(visits.Object, deletions.Object);

        ApplicationResult<VisitDeletionPreview> result = await handler.HandleAsync(
            new GetVisitDeletionPreviewQuery(" owner-1 ", visit.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(visit.Version, result.Value?.ExpectedVersion);
        Assert.Equal(4, result.Value?.OccurrenceCount);
        Assert.Equal(3, result.Value?.AssessmentCount);
        Assert.Equal(VisitDeletionPolicy.RetentionDays, result.Value?.RetentionDays);
        visits.VerifyAll();
        deletions.VerifyAll();
    }

    [Fact]
    public async Task Delete_TombstonesTheVisitInvalidatesExportsAndSchedulesThePurge()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync((VisitDeletionReceipt?)null);
        deletions.Setup(store => store.GetImpactAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionImpact(2, 1));
        VisitDeletionTombstoneRequest? tombstone = null;
        deletions.Setup(store => store.TryTombstoneAsync(
                It.IsAny<VisitDeletionTombstoneRequest>(),
                CancellationToken.None))
            .Callback<VisitDeletionTombstoneRequest, CancellationToken>(
                (request, _) => tombstone = request)
            .ReturnsAsync(true);
        deletions.Setup(store => store.MarkPurgeJobEnsuredAsync(
                visit.Id,
                visit.UserId,
                visit.Version + 1,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        deletions.Setup(store => store.TryClaimExportInvalidationAsync(
                visit.Id,
                visit.UserId,
                visit.Version + 1,
                NowUtc,
                NowUtc.Add(VisitDeletionPolicy.ExportInvalidationClaimDuration),
                CancellationToken.None))
            .ReturnsAsync(new VisitExportInvalidationClaim("claim-1", NowUtc));
        deletions.Setup(store => store.CompleteExportInvalidationAsync(
                visit.Id,
                visit.UserId,
                visit.Version + 1,
                "claim-1",
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports = new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.InvalidateOwnedAsync(
                visit.UserId,
                NowUtc,
                NowUtc,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IVisitContentMutationLease> lease = CreateLease();
        Mock<IVisitContentMutationLeaseManager> leaseManager =
            CreateLeaseManager(visit, lease.Object);
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.Kind == VisitPurgeJob.Kind
                    && request.Delay == VisitDeletionPolicy.Retention
                    && request.NaturalKey.EndsWith(":2", StringComparison.Ordinal)
                    && request.RequestedRevision == 0),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IPassportAuditPublisher> audits = new Mock<IPassportAuditPublisher>(MockBehavior.Strict);
        audits.Setup(publisher => publisher.TryPublishAsync(
                It.Is<PassportAuditEvent>(audit => audit.EventType == PassportAuditEventType.VisitDeleted),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            visits.Object,
            deletions.Object,
            exports.Object,
            leaseManager.Object,
            CreatePendingMutationReconciler(visit).Object,
            new VisitPurgeScheduler(jobs.Object),
            audits.Object,
            clock.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.WasReplayed);
        Assert.True(result.Value?.IsExportInvalidationEnsured);
        Assert.Equal(NowUtc.AddDays(VisitDeletionPolicy.RetentionDays), result.Value?.PurgeScheduledForUtc);
        Assert.NotNull(tombstone);
        Assert.Equal(2, tombstone.ExpectedVersion + 1);
        Assert.Equal(PassportAuditEventType.VisitDeleted, tombstone.AuditEvent.EventType);
        exports.Verify(repository => repository.InvalidateOwnedAsync(
            visit.UserId,
            NowUtc,
            NowUtc,
            CancellationToken.None), Times.Once);
        lease.Verify(value => value.MarkMutationCompleted(), Times.Once);
        lease.Verify(value => value.DisposeAsync(), Times.Once);
        leaseManager.VerifyAll();
        visits.VerifyAll();
        deletions.VerifyAll();
        jobs.VerifyAll();
        audits.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenTheImpactChanged_ReturnsConflictWithoutMutation()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync((VisitDeletionReceipt?)null);
        deletions.Setup(store => store.GetImpactAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionImpact(3, 1));
        DeleteVisitCommandHandler handler = CreateDeleteHandler(
            visit,
            visits.Object,
            deletions.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.deletion-preview-changed", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        deletions.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenPendingMutationsCannotBeReconciled_ShouldNotCountOrTombstone()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync((VisitDeletionReceipt?)null);
        Mock<IPassportPendingMutationReconciler> reconciler =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        reconciler.Setup(value => value.ReconcileBeforeLifecycleTransitionAsync(
                visit,
                CancellationToken.None))
            .ReturnsAsync(false);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            visits.Object,
            deletions.Object,
            new Mock<IPassportExportRepository>(MockBehavior.Strict).Object,
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict).Object,
            reconciler.Object,
            new VisitPurgeScheduler(
                new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict).Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            new Mock<IPassportClock>(MockBehavior.Strict).Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.version-conflict", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        deletions.VerifyAll();
        reconciler.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenInvalidationWasEnsured_ReplaysWithoutInvalidatingNewerExports()
    {
        Visit visit = CreateVisit();
        VisitDeletionReceipt storedReceipt = new VisitDeletionReceipt(
            visit.Id.Value,
            NowUtc,
            NowUtc.Add(VisitDeletionPolicy.Retention),
            2,
            false,
            true);
        Mock<IVisitDeletionStore> deletions = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync(storedReceipt);
        deletions.Setup(store => store.MarkPurgeJobEnsuredAsync(
                visit.Id,
                visit.UserId,
                storedReceipt.DeletionVersion,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == "passport-visit-purge:visit-1:2"
                    && request.RequestedRevision == 0
                    && request.Delay == VisitDeletionPolicy.Retention),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            new Mock<IUserVisitRepository>(MockBehavior.Strict).Object,
            deletions.Object,
            exports.Object,
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict).Object,
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict).Object,
            new VisitPurgeScheduler(jobs.Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            clock.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal(storedReceipt.PurgeScheduledForUtc, result.Value?.PurgeScheduledForUtc);
        deletions.VerifyAll();
        exports.VerifyNoOtherCalls();
        jobs.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenAnotherRequestOwnsTheInvalidationClaim_ShouldNotInvalidateAgain()
    {
        Visit visit = CreateVisit();
        VisitDeletionReceipt storedReceipt = new VisitDeletionReceipt(
            visit.Id.Value,
            NowUtc,
            NowUtc.Add(VisitDeletionPolicy.Retention),
            2,
            false);
        Mock<IVisitDeletionStore> deletions =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync(storedReceipt);
        deletions.Setup(store => store.TryClaimExportInvalidationAsync(
                visit.Id,
                visit.UserId,
                storedReceipt.DeletionVersion,
                NowUtc,
                NowUtc.Add(VisitDeletionPolicy.ExportInvalidationClaimDuration),
                CancellationToken.None))
            .ReturnsAsync((VisitExportInvalidationClaim?)null);
        deletions.Setup(store => store.MarkPurgeJobEnsuredAsync(
                visit.Id,
                visit.UserId,
                storedReceipt.DeletionVersion,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.IsAny<CoalesceBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            new Mock<IUserVisitRepository>(MockBehavior.Strict).Object,
            deletions.Object,
            exports.Object,
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict).Object,
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict).Object,
            new VisitPurgeScheduler(jobs.Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            clock.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        exports.VerifyNoOtherCalls();
        deletions.VerifyAll();
        jobs.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenTheTombstoneLosesTheRace_DoesNotInvalidateOrSchedule()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.Setup(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync((VisitDeletionReceipt?)null);
        deletions.Setup(store => store.GetImpactAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionImpact(2, 1));
        deletions.Setup(store => store.TryTombstoneAsync(
                It.IsAny<VisitDeletionTombstoneRequest>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<IVisitContentMutationLease> lease = CreateLease();
        Mock<IVisitContentMutationLeaseManager> leaseManager =
            CreateLeaseManager(visit, lease.Object);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            visits.Object,
            deletions.Object,
            new Mock<IPassportExportRepository>(MockBehavior.Strict).Object,
            leaseManager.Object,
            CreatePendingMutationReconciler(visit).Object,
            new VisitPurgeScheduler(
                new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict).Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            clock.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.version-conflict", Assert.Single(result.Errors).Code);
        lease.Verify(value => value.MarkMutationCompleted(), Times.Once);
        lease.Verify(value => value.DisposeAsync(), Times.Once);
        visits.VerifyAll();
        deletions.VerifyAll();
        deletions.Verify(store => store.GetReceiptAsync(
            visit.Id,
            visit.UserId,
            "delete-1",
            CancellationToken.None), Times.Exactly(2));
        leaseManager.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenAnIdenticalConcurrentRequestWins_ShouldReplayItsReceipt()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 5), NowUtc);
        VisitDeletionReceipt concurrentReceipt = new VisitDeletionReceipt(
            visit.Id.Value,
            NowUtc,
            NowUtc.Add(VisitDeletionPolicy.Retention),
            visit.Version + 1,
            false);
        Mock<IUserVisitRepository> visits = CreateVisitRepository(visit);
        Mock<IVisitDeletionStore> deletions =
            new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        deletions.SetupSequence(store => store.GetReceiptAsync(
                visit.Id,
                visit.UserId,
                "delete-1",
                CancellationToken.None))
            .ReturnsAsync((VisitDeletionReceipt?)null)
            .ReturnsAsync(concurrentReceipt);
        deletions.Setup(store => store.GetImpactAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(new VisitDeletionImpact(2, 1));
        deletions.Setup(store => store.TryTombstoneAsync(
                It.IsAny<VisitDeletionTombstoneRequest>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        deletions.Setup(store => store.TryClaimExportInvalidationAsync(
                visit.Id,
                visit.UserId,
                concurrentReceipt.DeletionVersion,
                NowUtc,
                NowUtc.Add(VisitDeletionPolicy.ExportInvalidationClaimDuration),
                CancellationToken.None))
            .ReturnsAsync(new VisitExportInvalidationClaim("claim-1", NowUtc));
        deletions.Setup(store => store.CompleteExportInvalidationAsync(
                visit.Id,
                visit.UserId,
                concurrentReceipt.DeletionVersion,
                "claim-1",
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        deletions.Setup(store => store.MarkPurgeJobEnsuredAsync(
                visit.Id,
                visit.UserId,
                concurrentReceipt.DeletionVersion,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportExportRepository> exports =
            new Mock<IPassportExportRepository>(MockBehavior.Strict);
        exports.Setup(repository => repository.InvalidateOwnedAsync(
                visit.UserId,
                NowUtc,
                NowUtc,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey
                        == $"passport-visit-purge:{visit.Id.Value}:{concurrentReceipt.DeletionVersion}"
                    && request.RequestedRevision == 0
                    && request.Delay == VisitDeletionPolicy.Retention),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        DeleteVisitCommandHandler handler = new DeleteVisitCommandHandler(
            visits.Object,
            deletions.Object,
            exports.Object,
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict).Object,
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict).Object,
            new VisitPurgeScheduler(jobs.Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            clock.Object);

        ApplicationResult<VisitDeletionReceipt> result = await handler.HandleAsync(
            new DeleteVisitCommand(
                visit.UserId,
                visit.Id.Value,
                visit.Version,
                2,
                1,
                "delete-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal(concurrentReceipt.DeletionVersion, result.Value?.DeletionVersion);
        visits.VerifyAll();
        deletions.VerifyAll();
        exports.VerifyAll();
        jobs.VerifyAll();
        clock.VerifyAll();
    }

    private static DeleteVisitCommandHandler CreateDeleteHandler(
        Visit visit,
        IUserVisitRepository visits,
        IVisitDeletionStore deletions)
    {
        Mock<IVisitContentMutationLease> lease = CreateLease();
        Mock<IVisitContentMutationLeaseManager> leaseManager =
            CreateLeaseManager(visit, lease.Object);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        return new DeleteVisitCommandHandler(
            visits,
            deletions,
            new Mock<IPassportExportRepository>(MockBehavior.Strict).Object,
            leaseManager.Object,
            CreatePendingMutationReconciler(visit).Object,
            new VisitPurgeScheduler(
                new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict).Object),
            new Mock<IPassportAuditPublisher>(MockBehavior.Strict).Object,
            clock.Object);
    }

    private static Mock<IVisitContentMutationLeaseManager> CreateLeaseManager(
        Visit visit,
        IVisitContentMutationLease lease)
    {
        Mock<IVisitContentMutationLeaseManager> manager =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        manager.Setup(value => value.TryAcquireAsync(
                It.Is<Visit>(candidate =>
                    candidate.Id == visit.Id
                    && candidate.UserId == visit.UserId),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease);
        return manager;
    }

    private static Mock<IPassportPendingMutationReconciler>
        CreatePendingMutationReconciler(Visit visit)
    {
        Mock<IPassportPendingMutationReconciler> reconciler =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        reconciler.Setup(value => value.ReconcileBeforeLifecycleTransitionAsync(
                visit,
                CancellationToken.None))
            .ReturnsAsync(true);
        return reconciler;
    }

    private static Mock<IVisitContentMutationLease> CreateLease()
    {
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Loose);
        lease.SetupGet(value => value.Token).Returns("lease-1");
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return lease;
    }

    private static Mock<IUserVisitRepository> CreateVisitRepository(Visit visit)
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        return visits;
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 4),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Journée",
            "Souvenir privé",
            NowUtc.AddDays(-1));
    }
}
