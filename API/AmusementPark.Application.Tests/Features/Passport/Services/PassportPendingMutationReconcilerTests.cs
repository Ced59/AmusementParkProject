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
    public async Task ReconcileBatchAsync_WhenVisitIsDraft_ShouldFenceRecoveryWithLease()
    {
        Visit visit = CreateVisit();
        PendingPassportMutationVisit candidate =
            new PendingPassportMutationVisit(visit.UserId, visit.Id);
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
        occurrences.Setup(value => value.TryCompletePendingMutationAsync(
                visit.UserId,
                visit.Id,
                CancellationToken.None))
            .Callback(() => Assert.True(leaseWasAcquired))
            .ReturnsAsync(true);
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
                new PendingPassportMutationVisit(visit.UserId, visit.Id),
            });
        visits.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        PassportPendingMutationReconciler reconciler = new PassportPendingMutationReconciler(
            visits.Object,
            occurrences.Object,
            leases.Object,
            clock.Object);

        int reconciled = await reconciler.ReconcileBatchAsync(
            50,
            CancellationToken.None);

        Assert.Equal(0, reconciled);
        visits.VerifyAll();
        occurrences.VerifyAll();
        leases.VerifyNoOtherCalls();
        clock.VerifyNoOtherCalls();
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
}
