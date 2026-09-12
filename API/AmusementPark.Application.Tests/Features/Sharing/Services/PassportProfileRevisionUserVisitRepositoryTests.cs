using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileRevisionUserVisitRepositoryTests
{
    [Fact]
    public async Task CreateIdempotentAsync_WhenVisitIsDraft_ShouldNotTouchThePublicRevision()
    {
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 12),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        IdempotentVisitCreationResult creation = new IdempotentVisitCreationResult(
            IdempotentVisitCreationStatus.Created,
            visit);
        Mock<IUserVisitRepository> inner = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        inner.Setup(value => value.CreateIdempotentAsync(
                visit,
                "operation-1",
                CancellationToken.None))
            .ReturnsAsync(creation);
        PassportProfileRevisionUserVisitRepository repository =
            new PassportProfileRevisionUserVisitRepository(inner.Object, guard.Object);

        IdempotentVisitCreationResult result = await repository.CreateIdempotentAsync(
            visit,
            "operation-1",
            CancellationToken.None);

        Assert.Same(creation, result);
        inner.VerifyAll();
        guard.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_WhenVisitIsCompleted_ShouldAdvanceThePassportScope()
    {
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            new VisitDate(2026, 9, 12, VisitDatePrecision.Day, false),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        visit.Complete(
            new DateOnly(2026, 9, 12),
            new DateTime(2026, 9, 12, 10, 1, 0, DateTimeKind.Utc));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.CreateCoordination(visit.UserId));
        IReadOnlyCollection<ShareSourceMutationLease> leases = new[] { lease };
        Mock<IUserVisitRepository> inner = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        inner.InSequence(sequence)
            .Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        guard.InSequence(sequence)
            .Setup(value => value.TryBeginMutationAsync(
                visit.UserId,
                It.Is<IReadOnlyCollection<(string ParkId, int Year)>>(segments =>
                    segments.Count == 1
                    && segments.First().ParkId == "park-1"
                    && segments.First().Year == 2026),
                CancellationToken.None))
            .ReturnsAsync(leases);
        inner.InSequence(sequence)
            .Setup(value => value.TryUpdateOwnedAsync(visit, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        guard.InSequence(sequence)
            .Setup(value => value.CompleteMutationAsync(leases, true, CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionUserVisitRepository repository =
            new PassportProfileRevisionUserVisitRepository(inner.Object, guard.Object);

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.True(updated);
        inner.VerifyAll();
        guard.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_WhenWriteOutcomeIsAmbiguous_ShouldAdvanceThePassportScope()
    {
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            new VisitDate(2026, 9, 12, VisitDatePrecision.Day, false),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        visit.Complete(
            new DateOnly(2026, 9, 12),
            new DateTime(2026, 9, 12, 10, 1, 0, DateTimeKind.Utc));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.CreateCoordination(visit.UserId));
        IReadOnlyCollection<ShareSourceMutationLease> leases = new[] { lease };
        Mock<IUserVisitRepository> inner = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        inner.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        guard.Setup(value => value.TryBeginMutationAsync(
                visit.UserId,
                It.IsAny<IReadOnlyCollection<(string ParkId, int Year)>>(),
                CancellationToken.None))
            .ReturnsAsync(leases);
        inner.Setup(value => value.TryUpdateOwnedAsync(visit, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        guard.Setup(value => value.CompleteMutationAsync(leases, true, CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionUserVisitRepository repository =
            new PassportProfileRevisionUserVisitRepository(inner.Object, guard.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None));

        inner.VerifyAll();
        guard.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_WhenVisitRemainsDraft_ShouldNotTouchThePublicRevision()
    {
        Visit visit = Visit.Create(
            VisitId.New(),
            "owner-1",
            "park-1",
            new VisitDate(2026, 9, 12, VisitDatePrecision.Day, false),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        Mock<IUserVisitRepository> inner = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        inner.Setup(value => value.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        inner.Setup(value => value.TryUpdateOwnedAsync(
                visit,
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        PassportProfileRevisionUserVisitRepository repository =
            new PassportProfileRevisionUserVisitRepository(inner.Object, guard.Object);

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.True(updated);
        inner.VerifyAll();
        guard.VerifyNoOtherCalls();
    }
}
