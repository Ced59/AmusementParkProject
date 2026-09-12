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
    public async Task TryBeginMutationAsync_ShouldUseTheSelectedPassportSegmentScope()
    {
        const string ownerUserId = "owner-1";
        string scopeKey = PassportProfileShareSourceScope.CreateSegment(
            ownerUserId,
            2026,
            "park-1");
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(scopeKey);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.TryBeginMutationAsync(scopeKey, CancellationToken.None))
            .ReturnsAsync(lease);
        PassportProfileShareSourceRevisionGuard guard =
            new PassportProfileShareSourceRevisionGuard(
                revisions.Object,
                NullLogger<PassportProfileShareSourceRevisionGuard>.Instance);

        IReadOnlyCollection<ShareSourceMutationLease> result = await guard.TryBeginMutationAsync(
            ownerUserId,
            new[] { ("park-1", 2026) },
            CancellationToken.None);

        Assert.Same(lease, Assert.Single(result));
        revisions.VerifyAll();
    }
}
