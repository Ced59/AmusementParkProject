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
    public async Task TryUpdateOwnedAsync_ShouldAdvanceThePassportScopeAfterAWrite()
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
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            PassportProfileShareSourceScope.Create(visit.UserId));
        Mock<IUserVisitRepository> inner = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IPassportProfileShareSourceRevisionGuard> guard =
            new Mock<IPassportProfileShareSourceRevisionGuard>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        guard.InSequence(sequence)
            .Setup(value => value.TryBeginMutationAsync(visit.UserId, CancellationToken.None))
            .ReturnsAsync(lease);
        inner.InSequence(sequence)
            .Setup(value => value.TryUpdateOwnedAsync(visit, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        guard.InSequence(sequence)
            .Setup(value => value.CompleteMutationAsync(lease, true, CancellationToken.None))
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
}
