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
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.Create(occurrence.UserId));
        Mock<IRideOccurrenceRepository> inner =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        guard.InSequence(sequence)
            .Setup(value => value.TryBeginMutationAsync(
                occurrence.UserId,
                CancellationToken.None))
            .ReturnsAsync(lease);
        inner.InSequence(sequence)
            .Setup(value => value.TryDeleteOwnedAsync(
                occurrence,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        guard.InSequence(sequence)
            .Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionRideOccurrenceRepository repository =
            new PassportProfileRevisionRideOccurrenceRepository(inner.Object, guard.Object);

        bool deleted = await repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None);

        Assert.True(deleted);
        inner.VerifyAll();
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
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.Create(occurrence.UserId));
        Mock<IRideOccurrenceRepository> inner =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        guard.Setup(value => value.TryBeginMutationAsync(
                occurrence.UserId,
                CancellationToken.None))
            .ReturnsAsync(lease);
        inner.Setup(value => value.TryDeleteOwnedAsync(
                occurrence,
                1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        guard.Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionRideOccurrenceRepository repository =
            new PassportProfileRevisionRideOccurrenceRepository(inner.Object, guard.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None));

        inner.VerifyAll();
        guard.VerifyAll();
    }
}
