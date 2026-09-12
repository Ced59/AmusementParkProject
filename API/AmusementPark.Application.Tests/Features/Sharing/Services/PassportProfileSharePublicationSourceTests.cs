using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSourceTests
{
    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldReadOnlyConstantTimeRevisionDocuments()
    {
        const string ownerUserId = "owner-1";
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 3
                    && scopes.Contains(passportScope)
                    && scopes.Contains(identityScope)
                    && scopes.Contains(PersonalRankingShareSourceScope.PublicCatalog)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [identityScope] = Revision(7),
                [PersonalRankingShareSourceScope.PublicCatalog] = Revision(3),
            });
        PassportProfileSharePublicationSource source = new PassportProfileSharePublicationSource(
            revisions.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            passportScope,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareOwnedSourceRevisionSnapshotAsync_ShouldArmThePassportRevision()
    {
        const string ownerUserId = "owner-1";
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetOrCreateAsync(
                passportScope,
                CancellationToken.None))
            .ReturnsAsync(Revision(4));
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 3
                    && scopes.Contains(passportScope)
                    && scopes.Contains(identityScope)
                    && scopes.Contains(PersonalRankingShareSourceScope.PublicCatalog)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [identityScope] = Revision(7),
                [PersonalRankingShareSourceScope.PublicCatalog] = Revision(3),
            });
        PassportProfileSharePublicationSource source = new PassportProfileSharePublicationSource(
            revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevisionSnapshot> result =
            await source.PrepareOwnedSourceRevisionSnapshotAsync(
                ownerUserId,
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Passport.Revision);
        Assert.Equal(7, result.Value.Identity.Revision);
        Assert.Equal(3, result.Value.Catalog.Revision);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileOwnedSourceVersionAsync_ShouldRejectAConcurrentCatalogChange()
    {
        const string ownerUserId = "owner-1";
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        PassportProfileShareSourceRevisionSnapshot expected = new PassportProfileShareSourceRevisionSnapshot(
            Revision(4),
            Revision(7),
            Revision(3));
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.ReconcileFingerprintAsync(
                passportScope,
                "source-fingerprint",
                CancellationToken.None))
            .ReturnsAsync(Revision(5));
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(5),
                [identityScope] = Revision(7),
                [PersonalRankingShareSourceScope.PublicCatalog] = Revision(4),
            });
        PassportProfileSharePublicationSource source = new PassportProfileSharePublicationSource(
            revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevision> result =
            await source.ReconcileOwnedSourceVersionAsync(
                ownerUserId,
                "source-fingerprint",
                expected,
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.source-changed");
        revisions.VerifyAll();
    }

    private static ShareSourceRevision Revision(long value)
    {
        return new ShareSourceRevision(
            value,
            0,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
    }
}
