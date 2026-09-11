using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileRevisionVisitDeletionStoreTests
{
    [Fact]
    public async Task TryTombstoneAsync_ShouldAdvanceThePassportScopeAfterDeletion()
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
        VisitDeletionTombstoneRequest request = new VisitDeletionTombstoneRequest(
            visit.Id,
            visit.UserId,
            visit.Version,
            "operation-1",
            nowUtc,
            nowUtc.AddDays(30),
            null,
            VisitDeletionAuditEventFactory.Create(visit, nowUtc));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.Create(visit.UserId));
        Mock<IVisitDeletionStore> inner = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        guard.InSequence(sequence)
            .Setup(value => value.TryBeginMutationAsync(visit.UserId, CancellationToken.None))
            .ReturnsAsync(lease);
        inner.InSequence(sequence)
            .Setup(value => value.TryTombstoneAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        guard.InSequence(sequence)
            .Setup(value => value.CompleteMutationAsync(lease, true, CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionVisitDeletionStore store =
            new PassportProfileRevisionVisitDeletionStore(inner.Object, guard.Object);

        bool deleted = await store.TryTombstoneAsync(request, CancellationToken.None);

        Assert.True(deleted);
        inner.VerifyAll();
        guard.VerifyAll();
    }

    [Fact]
    public async Task TryTombstoneAsync_WhenWriteOutcomeIsAmbiguous_ShouldAdvanceThePassportScope()
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
        VisitDeletionTombstoneRequest request = new VisitDeletionTombstoneRequest(
            visit.Id,
            visit.UserId,
            visit.Version,
            "operation-1",
            nowUtc,
            nowUtc.AddDays(30),
            null,
            VisitDeletionAuditEventFactory.Create(visit, nowUtc));
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.Create(visit.UserId));
        Mock<IVisitDeletionStore> inner = new Mock<IVisitDeletionStore>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        guard.Setup(value => value.TryBeginMutationAsync(visit.UserId, CancellationToken.None))
            .ReturnsAsync(lease);
        inner.Setup(value => value.TryTombstoneAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        guard.Setup(value => value.CompleteMutationAsync(lease, true, CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileRevisionVisitDeletionStore store =
            new PassportProfileRevisionVisitDeletionStore(inner.Object, guard.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => store.TryTombstoneAsync(
            request,
            CancellationToken.None));

        inner.VerifyAll();
        guard.VerifyAll();
    }
}
