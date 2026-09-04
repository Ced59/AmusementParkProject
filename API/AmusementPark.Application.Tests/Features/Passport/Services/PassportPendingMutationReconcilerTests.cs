using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportPendingMutationReconcilerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryAcquireReconciledLifecycleLeaseAsync_ShouldTransferTheActiveLeaseToTheCaller()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        occurrences.Setup(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync((PendingPassportMutationVisit?)null);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        IVisitContentMutationLease? acquired =
            await reconciler.TryAcquireReconciledLifecycleLeaseAsync(
                visit,
                CancellationToken.None);

        Assert.Same(lease.Object, acquired);
        lease.Verify(value => value.MarkMutationCompleted(), Times.Never);
        lease.Verify(value => value.DisposeAsync(), Times.Never);
        visits.VerifyNoOtherCalls();
        occurrences.VerifyAll();
        leases.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBeforeLifecycleTransitionAsync_ShouldSettleAllPendingOperationsUnderLease()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit creation =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Creation,
                CreatePreparation(visit),
                7);
        PendingPassportMutationVisit deletion =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-2",
                PendingPassportMutationKind.Delete,
                null,
                7);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        bool leaseWasAcquired = false;
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .Callback(() => leaseWasAcquired = true)
            .ReturnsAsync(lease.Object);
        occurrences.SetupSequence(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync(creation)
            .ReturnsAsync(deletion)
            .ReturnsAsync((PendingPassportMutationVisit?)null);
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                creation,
                CancellationToken.None))
            .Callback(() => Assert.True(leaseWasAcquired))
            .ReturnsAsync(true);
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                deletion,
                CancellationToken.None))
            .Callback(() => Assert.True(leaseWasAcquired))
            .ReturnsAsync(true);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        bool reconciled = await reconciler.ReconcileBeforeLifecycleTransitionAsync(
            visit,
            CancellationToken.None);

        Assert.True(reconciled);
        visits.VerifyNoOtherCalls();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBeforeLifecycleTransitionAsync_WhenRecoverableOperationCannotComplete_ShouldRejectItAndContinue()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit creation =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Creation,
                CreatePreparation(visit),
                7);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        occurrences.SetupSequence(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync(creation)
            .ReturnsAsync((PendingPassportMutationVisit?)null);
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                creation,
                CancellationToken.None))
            .ReturnsAsync(false);
        occurrences.Setup(value => value.TryRejectPendingMutationAsync(
                creation,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        bool reconciled = await reconciler.ReconcileBeforeLifecycleTransitionAsync(
            visit,
            CancellationToken.None);

        Assert.True(reconciled);
        visits.VerifyNoOtherCalls();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBeforeLifecycleTransitionAsync_WhenReorderCannotComplete_ShouldKeepItPending()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit reorder =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Reorder,
                null,
                7);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        occurrences.Setup(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync(reorder);
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                reorder,
                CancellationToken.None))
            .ReturnsAsync(false);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        bool reconciled = await reconciler.ReconcileBeforeLifecycleTransitionAsync(
            visit,
            CancellationToken.None);

        Assert.False(reconciled);
        visits.VerifyNoOtherCalls();
        occurrences.VerifyAll();
        occurrences.Verify(value => value.TryRejectPendingMutationAsync(
            It.IsAny<PendingPassportMutationVisit>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenVisitIsDraft_ShouldFenceRecoveryWithLease()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit candidate =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Creation,
                CreatePreparation(visit),
                6);
        PendingPassportMutationVisit fencedCandidate = candidate with
        {
            ContentFenceToken = 7,
        };
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        bool leaseWasAcquired = false;
        occurrences.Setup(value => value.ListPendingAuditMutationVisitsAsync(
                50,
                CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        visits.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .Callback(() => leaseWasAcquired = true)
            .ReturnsAsync(lease.Object);
        occurrences.Setup(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync(fencedCandidate);
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                fencedCandidate,
                CancellationToken.None))
            .Callback(() => Assert.True(leaseWasAcquired))
            .ReturnsAsync(true);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        int reconciled = await reconciler.ReconcileBatchAsync(
            50,
            CancellationToken.None);

        Assert.Equal(1, reconciled);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenVisitIsLocked_ShouldNotRecoverContent()
    {
        Visit visit = CreateVisit();
        visit.Complete(new DateOnly(2026, 9, 3), NowUtc.AddMinutes(1));
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        occurrences.Setup(value => value.ListPendingAuditMutationVisitsAsync(
                50,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new PendingPassportMutationVisit(
                    visit.UserId,
                    visit.Id,
                    "operation-1",
                    PendingPassportMutationKind.Reorder,
                    null),
            });
        visits.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        occurrences.Setup(value => value.TryRejectPendingMutationAsync(
                It.Is<PendingPassportMutationVisit>(candidate =>
                    candidate.OperationKeyHash == "operation-1"),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        int reconciled = await reconciler.ReconcileBatchAsync(
            50,
            CancellationToken.None);

        Assert.Equal(1, reconciled);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyNoOtherCalls();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenCandidateWasNotPromoted_ShouldRejectItsOldFence()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit staleCandidate =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Creation,
                CreatePreparation(visit),
                5);
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        occurrences.Setup(value => value.ListPendingAuditMutationVisitsAsync(
                50,
                CancellationToken.None))
            .ReturnsAsync(new[] { staleCandidate });
        visits.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        occurrences.Setup(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync((PendingPassportMutationVisit?)null);
        occurrences.Setup(value => value.TryRejectPendingMutationAsync(
                staleCandidate,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        int reconciled = await reconciler.ReconcileBatchAsync(
            50,
            CancellationToken.None);

        Assert.Equal(1, reconciled);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenCreationIdentityChanged_ShouldRejectUnderLease()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit candidate =
            new PendingPassportMutationVisit(
                visit.UserId,
                visit.Id,
                "operation-1",
                PendingPassportMutationKind.Creation,
                new RideOccurrenceCreationPreparation(
                    visit.ParkId,
                    VisitDate.ForDay(2026, 9, 2),
                    visit.TimeZoneId,
                    visit.ServiceDayConvention,
                    new[] { HistoricalConsistency.Verified }),
                6);
        PendingPassportMutationVisit fencedCandidate = candidate with
        {
            ContentFenceToken = 7,
        };
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IVisitContentMutationLease> lease =
            new Mock<IVisitContentMutationLease>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        occurrences.Setup(value => value.ListPendingAuditMutationVisitsAsync(
                50,
                CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        visits.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        leases.Setup(value => value.TryAcquireAsync(
                visit,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(lease.Object);
        occurrences.Setup(value => value.GetPendingMutationFencedAsync(
                visit.UserId,
                visit.Id,
                7,
                CancellationToken.None))
            .ReturnsAsync(fencedCandidate);
        occurrences.Setup(value => value.TryRejectPendingMutationAsync(
                fencedCandidate,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);
        lease.SetupGet(value => value.ContentFenceToken).Returns(7);
        lease.Setup(value => value.MarkMutationCompleted());
        lease.Setup(value => value.DisposeAsync()).Returns(ValueTask.CompletedTask);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        int reconciled = await reconciler.ReconcileBatchAsync(
            50,
            CancellationToken.None);

        Assert.Equal(1, reconciled);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyAll();
        lease.VerifyAll();
        clock.VerifyAll();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task ReconcileBatchAsync_WhenLimitIsOutsideBound_ShouldReject(int limit)
    {
        Mock<IUserVisitRepository> visits =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IRideOccurrenceRepository> occurrences =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IVisitContentMutationLeaseManager> leases =
            new Mock<IVisitContentMutationLeaseManager>(MockBehavior.Strict);
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            reconciler.ReconcileBatchAsync(limit, CancellationToken.None));

        visits.VerifyNoOtherCalls();
        occurrences.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
        clock.VerifyNoOtherCalls();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
    }

    private static RideOccurrenceCreationPreparation CreatePreparation(Visit visit)
    {
        return new RideOccurrenceCreationPreparation(
            visit.ParkId,
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            new[] { HistoricalConsistency.Verified });
    }
}
