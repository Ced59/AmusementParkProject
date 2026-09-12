using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSourceTests
{
    private const string OwnerUserId = "owner-1";
    private const string ParkId = "park-1";

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldReadOnlySelectedDependencies()
    {
        string passportScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePark(ParkId);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 3
                    && scopes.Contains(passportScope)
                    && scopes.Contains(displayNameScope)
                    && scopes.Contains(catalogScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [displayNameScope] = Revision(7),
                [catalogScope] = Revision(3),
            });
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            new SharePublicationSourceVersionRequest(
                passportScope,
                CreatePolicy(ShareContentField.PublicDisplayName),
                SelectedParkIds: new[] { ParkId }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareOwnedSourceRevisionSnapshotAsync_ShouldArmPassportAndReadOptionalScopes()
    {
        string passportScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(OwnerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.Create(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePark(ParkId);
        ShareContentPolicy policy = CreatePolicy(
            ShareContentField.PublicDisplayName,
            ShareContentField.Avatar,
            ShareContentField.GlobalRatings);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetOrCreateAsync(passportScope, CancellationToken.None))
            .ReturnsAsync(Revision(4));
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 5
                    && scopes.Contains(passportScope)
                    && scopes.Contains(displayNameScope)
                    && scopes.Contains(avatarScope)
                    && scopes.Contains(ratingsScope)
                    && scopes.Contains(catalogScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [displayNameScope] = Revision(7),
                [avatarScope] = Revision(9),
                [ratingsScope] = Revision(11),
                [catalogScope] = Revision(3),
            });
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevisionSnapshot> result =
            await source.PrepareOwnedSourceRevisionSnapshotAsync(
                OwnerUserId,
                policy,
                new[] { ParkId },
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Passport.Revision);
        Assert.Equal(7, result.Value.DisplayName.Revision);
        Assert.Equal(9, result.Value.Avatar.Revision);
        Assert.Equal(11, result.Value.Ratings.Revision);
        Assert.Equal(3, result.Value.Catalog[catalogScope].Revision);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileOwnedSourceVersionAsync_ShouldRejectAConcurrentSelectedParkChange()
    {
        string passportScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePark(ParkId);
        ShareContentPolicy policy = CreatePolicy(ShareContentField.PublicDisplayName);
        PassportProfileShareSourceRevisionSnapshot expected = new PassportProfileShareSourceRevisionSnapshot(
            Revision(4),
            Revision(7),
            Revision(0),
            Revision(0),
            new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [catalogScope] = Revision(3),
            });
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
                [displayNameScope] = Revision(7),
                [catalogScope] = Revision(4),
            });
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevision> result =
            await source.ReconcileOwnedSourceVersionAsync(
                OwnerUserId,
                "source-fingerprint",
                expected,
                policy,
                new[] { ParkId },
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.source-changed");
        revisions.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldIncludeOnlyPublishedIdentityAndRatings()
    {
        string passportScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(OwnerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.Create(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePark(ParkId);
        Dictionary<string, ShareSourceRevision> available = new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
        {
            [passportScope] = Revision(4),
            [displayNameScope] = Revision(7),
            [avatarScope] = Revision(9),
            [ratingsScope] = Revision(11),
            [catalogScope] = Revision(3),
        };
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync((IReadOnlyCollection<string> scopes, CancellationToken _) =>
                scopes.ToDictionary(scope => scope, scope => available[scope], StringComparer.Ordinal));
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<long> geographyOnly = await source.GetCurrentSourceVersionAsync(
            Request(passportScope, CreatePolicy(ShareContentField.GeographicStatistics)),
            CancellationToken.None);
        ApplicationResult<long> withName = await source.GetCurrentSourceVersionAsync(
            Request(passportScope, CreatePolicy(ShareContentField.PublicDisplayName)),
            CancellationToken.None);
        ApplicationResult<long> withAvatar = await source.GetCurrentSourceVersionAsync(
            Request(passportScope, CreatePolicy(ShareContentField.Avatar)),
            CancellationToken.None);
        ApplicationResult<long> withRatings = await source.GetCurrentSourceVersionAsync(
            Request(passportScope, CreatePolicy(ShareContentField.GlobalRatings)),
            CancellationToken.None);

        Assert.Equal(7, geographyOnly.Value);
        Assert.Equal(14, withName.Value);
        Assert.Equal(16, withAvatar.Value);
        Assert.Equal(18, withRatings.Value);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldResolveSelectedParksFromPublishedSnapshot()
    {
        SharePublicationId publicationId = SharePublicationId.New();
        string passportScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePark(ParkId);
        ShareContentPolicy policy = CreatePolicy(ShareContentField.GeographicStatistics);
        PassportProfileShareInput selection = new PassportProfileShareInput(
            new[] { 2026 },
            new[] { ParkId },
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            false);
        PassportProfileShareSnapshot persistedSnapshot = new PassportProfileShareSnapshot(
            publicationId,
            2,
            3,
            7,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            "content-fingerprint",
            selection,
            CreateEmptyContent(),
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        Mock<IPassportProfileShareSnapshotRepository> snapshots =
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(publicationId, 2, CancellationToken.None))
            .ReturnsAsync(persistedSnapshot);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 2
                    && scopes.Contains(passportScope)
                    && scopes.Contains(catalogScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [catalogScope] = Revision(3),
            });
        PassportProfileSharePublicationSource source = new PassportProfileSharePublicationSource(
            revisions.Object,
            snapshots.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            new SharePublicationSourceVersionRequest(
                passportScope,
                policy,
                publicationId,
                2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
        revisions.VerifyAll();
        snapshots.VerifyAll();
    }

    private static PassportProfileSharePublicationSource CreateSource(
        IShareSourceRevisionRepository revisions)
    {
        return new PassportProfileSharePublicationSource(
            revisions,
            Mock.Of<IPassportProfileShareSnapshotRepository>());
    }

    private static SharePublicationSourceVersionRequest Request(
        string sourceScopeKey,
        ShareContentPolicy policy)
    {
        return new SharePublicationSourceVersionRequest(
            sourceScopeKey,
            policy,
            SelectedParkIds: new[] { ParkId });
    }

    private static ShareContentPolicy CreatePolicy(params ShareContentField[] fields)
    {
        return ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            fields);
    }

    private static PassportProfileSharePreviewResult CreateEmptyContent()
    {
        return new PassportProfileSharePreviewResult(
            null,
            null,
            null,
            ShareVisibility.Unlisted,
            false,
            0,
            0,
            null,
            null,
            null,
            null,
            Array.Empty<PassportProfileShareCountryResult>(),
            Array.Empty<PassportProfileShareYearResult>(),
            Array.Empty<PassportProfileShareParkResult>(),
            Array.Empty<PassportProfileShareRatingResult>(),
            Array.Empty<PassportProfileShareMissedItemResult>(),
            false,
            "passport-profile-v1",
            false);
    }

    private static ShareSourceRevision Revision(long value)
    {
        return new ShareSourceRevision(
            value,
            0,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
    }
}
