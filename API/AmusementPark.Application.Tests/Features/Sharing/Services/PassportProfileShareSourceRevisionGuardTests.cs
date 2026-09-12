using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileShareSourceRevisionGuardTests
{
    [Fact]
    public async Task TryBeginMutationAsync_ShouldFenceCoordinationAndMatchingPublishedScopes()
    {
        const string ownerUserId = "owner-1";
        string coordinationScope = PassportProfileShareSourceScope.CreateCoordination(ownerUserId);
        string publishedScope = PassportProfileShareSourceScope.CreateFingerprint(
            ownerUserId,
            new[] { 2026 },
            new[] { "park-1" });
        ShareSourceMutationLease coordinationLease = ShareSourceMutationLease.Create(
            coordinationScope);
        ShareSourceMutationLease publishedLease = ShareSourceMutationLease.Create(publishedScope);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginMutationAsync(
                coordinationScope,
                CancellationToken.None))
            .ReturnsAsync(coordinationLease);
        revisions.Setup(value => value.BeginMutationAsync(
                publishedScope,
                CancellationToken.None))
            .ReturnsAsync(publishedLease);
        Mock<IPassportProfileShareScopeRegistry> registry =
            new Mock<IPassportProfileShareScopeRegistry>(MockBehavior.Strict);
        registry.Setup(value => value.ResolveScopeKeysAsync(
                ownerUserId,
                It.Is<IReadOnlyCollection<(string ParkId, int Year)>>(segments =>
                    segments.Count == 1
                    && segments.First().ParkId == "park-1"
                    && segments.First().Year == 2026),
                CancellationToken.None))
            .ReturnsAsync(new[] { publishedScope });
        PassportProfileShareSourceRevisionGuard guard =
            new PassportProfileShareSourceRevisionGuard(
                revisions.Object,
                registry.Object,
                NullLogger<PassportProfileShareSourceRevisionGuard>.Instance);

        IReadOnlyCollection<ShareSourceMutationLease> result = await guard.TryBeginMutationAsync(
            ownerUserId,
            new[] { ("park-1", 2026) },
            CancellationToken.None);

        Assert.Equal(new[] { coordinationLease, publishedLease }, result);
        revisions.VerifyAll();
        registry.VerifyAll();
    }
}
