using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileRevisionRideOccurrenceRepositoryTests
{
    [Fact]
    public async Task TryDeleteOwnedAsync_ShouldAdvanceThePassportScopeAfterAWrite()
    {
        DateTime nowUtc = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            new VisitDate(2026, 9, 12, VisitDatePrecision.Day, false),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.New(),
            visit,
            "item-1",
            RideOccurrence.SortPositionStep,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
        visit.Complete(new DateOnly(2026, 9, 12), nowUtc.AddMinutes(1));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.CreateSegment(occurrence.UserId, 2026, "park-1"));
        IReadOnlyCollection<ShareSourceMutationLease> leases = new[] { lease };
        Mock<IRideOccurrenceRepository> inner =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        visitRepository.InSequence(sequence)
            .Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        guard.InSequence(sequence)
            .Setup(value => value.TryBeginMutationAsync(
                occurrence.UserId,
                It.Is<IReadOnlyCollection<(string ParkId, int Year)>>(segments =>
                    segments.Count == 1
                    && segments.First().ParkId == "park-1"
                    && segments.First().Year == 2026),
                CancellationToken.None))
            .ReturnsAsync(leases);
        inner.InSequence(sequence)
            .Setup(value => value.TryDeleteOwnedAsync(
                occurrence,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        guard.InSequence(sequence)
            .Setup(value => value.CompleteMutationAsync(
                leases,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionRideOccurrenceRepository repository =
            new PassportProfileRevisionRideOccurrenceRepository(
                inner.Object,
                visitRepository.Object,
                guard.Object);

        bool deleted = await repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None);

        Assert.True(deleted);
        inner.VerifyAll();
        visitRepository.VerifyAll();
        guard.VerifyAll();
    }

    [Fact]
    public async Task TryDeleteOwnedAsync_WhenWriteOutcomeIsAmbiguous_ShouldAdvanceThePassportScope()
    {
        DateTime nowUtc = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            new VisitDate(2026, 9, 12, VisitDatePrecision.Day, false),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.New(),
            visit,
            "item-1",
            RideOccurrence.SortPositionStep,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
        visit.Complete(new DateOnly(2026, 9, 12), nowUtc.AddMinutes(1));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.CreateSegment(occurrence.UserId, 2026, "park-1"));
        IReadOnlyCollection<ShareSourceMutationLease> leases = new[] { lease };
        Mock<IRideOccurrenceRepository> inner =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        guard.Setup(value => value.TryBeginMutationAsync(
                occurrence.UserId,
                It.IsAny<IReadOnlyCollection<(string ParkId, int Year)>>(),
                CancellationToken.None))
            .ReturnsAsync(leases);
        inner.Setup(value => value.TryDeleteOwnedAsync(
                occurrence,
                1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        guard.Setup(value => value.CompleteMutationAsync(
                leases,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionRideOccurrenceRepository repository =
            new PassportProfileRevisionRideOccurrenceRepository(
                inner.Object,
                visitRepository.Object,
                guard.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None));

        inner.VerifyAll();
        visitRepository.VerifyAll();
        guard.VerifyAll();
    }

    [Fact]
    public async Task TryDeleteOwnedAsync_WhenVisitIsDraft_ShouldNotTouchThePublicRevision()
    {
        DateTime nowUtc = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 12),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.New(),
            visit,
            "item-1",
            RideOccurrence.SortPositionStep,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
        Mock<IRideOccurrenceRepository> inner =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IUserVisitRepository> visitRepository =
            new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        visitRepository.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        inner.Setup(value => value.TryDeleteOwnedAsync(
                occurrence,
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        PassportProfileRevisionRideOccurrenceRepository repository =
            new PassportProfileRevisionRideOccurrenceRepository(
                inner.Object,
                visitRepository.Object,
                guard.Object);

        bool deleted = await repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None);

        Assert.True(deleted);
        inner.VerifyAll();
        visitRepository.VerifyAll();
        guard.VerifyNoOtherCalls();
    }
}
