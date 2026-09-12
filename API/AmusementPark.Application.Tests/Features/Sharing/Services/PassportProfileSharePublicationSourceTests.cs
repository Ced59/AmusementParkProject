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
        string sourceScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePassportGeographyPark(ParkId);
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
                sourceScope,
                CreatePolicy(
                    ShareContentField.PublicDisplayName,
                    ShareContentField.GeographicStatistics),
                PassportProfile: CreateInput()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareOwnedSourceRevisionSnapshotAsync_ShouldArmPassportAndReadOptionalScopes()
    {
        string coordinationScope = PassportProfileShareSourceScope.CreateCoordination(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(OwnerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.CreateRating(
            OwnerUserId,
            "Park:rating-1");
        string catalogScope = PublicCatalogShareSourceScope.CreatePassportRatingsPark(ParkId);
        ShareContentPolicy policy = CreatePolicy(
            ShareContentField.PublicDisplayName,
            ShareContentField.Avatar,
            ShareContentField.GlobalRatings);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.EnsureCreatedAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.SequenceEqual(
                    new[] { coordinationScope, passportScope })),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IPassportProfileShareScopeRegistry> registry =
            new Mock<IPassportProfileShareScopeRegistry>(MockBehavior.Strict);
        registry.Setup(value => value.RegisterAsync(
                OwnerUserId,
                passportScope,
                It.Is<IReadOnlyCollection<int>>(years => years.SequenceEqual(new[] { 2026 })),
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { ParkId })),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 6
                    && scopes.Contains(passportScope)
                    && scopes.Contains(coordinationScope)
                    && scopes.Contains(displayNameScope)
                    && scopes.Contains(avatarScope)
                    && scopes.Contains(ratingsScope)
                    && scopes.Contains(catalogScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [coordinationScope] = Revision(2),
                [passportScope] = Revision(4),
                [displayNameScope] = Revision(7),
                [avatarScope] = Revision(9),
                [ratingsScope] = Revision(11),
                [catalogScope] = Revision(3),
            });
        PassportProfileSharePublicationSource source = CreateSource(
            revisions.Object,
            registry.Object);

        ApplicationResult<PassportProfileShareSourceRevisionSnapshot> result =
            await source.PrepareOwnedSourceRevisionSnapshotAsync(
                OwnerUserId,
                policy,
                CreateInput("Park:rating-1"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Passport.Revision);
        Assert.Equal(7, result.Value.DisplayName.Revision);
        Assert.Equal(9, result.Value.Avatar.Revision);
        Assert.Equal(11, result.Value.Ratings.Revision);
        Assert.Equal(3, result.Value.Catalog[catalogScope].Revision);
        revisions.VerifyAll();
        registry.VerifyAll();
    }

    [Fact]
    public async Task PrepareOwnedSourceRevisionSnapshotAsync_WhenOnlyIdentityIsShared_ShouldNotArmPassportScopes()
    {
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 1
                    && scopes.Contains(displayNameScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [displayNameScope] = Revision(7),
            });
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevisionSnapshot> result =
            await source.PrepareOwnedSourceRevisionSnapshotAsync(
                OwnerUserId,
                CreatePolicy(ShareContentField.PublicDisplayName),
                CreateInput(),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Coordination.Revision);
        Assert.Equal(0, result.Value.Passport.Revision);
        Assert.Equal(7, result.Value.DisplayName.Revision);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileOwnedSourceVersionAsync_ShouldRejectAConcurrentSelectedParkChange()
    {
        string coordinationScope = PassportProfileShareSourceScope.CreateCoordination(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string catalogScope = PublicCatalogShareSourceScope.CreatePassportGeographyPark(ParkId);
        ShareContentPolicy policy = CreatePolicy(
            ShareContentField.PublicDisplayName,
            ShareContentField.GeographicStatistics);
        PassportProfileShareSourceRevisionSnapshot expected = new PassportProfileShareSourceRevisionSnapshot(
            Revision(2),
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
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [coordinationScope] = Revision(2),
                [passportScope] = Revision(4),
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
                CreateInput(),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.source-changed");
        revisions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileOwnedSourceVersionAsync_ShouldVersionAFirstSelectedSourceMutation()
    {
        string coordinationScope = PassportProfileShareSourceScope.CreateCoordination(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string catalogScope = PublicCatalogShareSourceScope.CreatePassportGeographyPark(ParkId);
        ShareContentPolicy policy = CreatePolicy(ShareContentField.GeographicStatistics);
        PassportProfileShareSourceRevisionSnapshot expected = new PassportProfileShareSourceRevisionSnapshot(
            Revision(4),
            Revision(0),
            Revision(0),
            Revision(0),
            Revision(0),
            new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [catalogScope] = Revision(0),
            });
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 3
                    && scopes.Contains(coordinationScope)
                    && scopes.Contains(passportScope)
                    && scopes.Contains(catalogScope)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [coordinationScope] = Revision(4),
                [passportScope] = Revision(0),
                [catalogScope] = Revision(0),
            });
        revisions.Setup(value => value.ReconcileFingerprintAsync(
                passportScope,
                "selected-source-fingerprint",
                CancellationToken.None))
            .ReturnsAsync(Revision(1));
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<PassportProfileShareSourceRevision> result =
            await source.ReconcileOwnedSourceVersionAsync(
                OwnerUserId,
                "selected-source-fingerprint",
                expected,
                policy,
                CreateInput(),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Version);
        Assert.Equal("selected-source-fingerprint", result.Value.SourceFingerprint);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldIncludeOnlyPublishedIdentityAndRatings()
    {
        string sourceScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(OwnerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(OwnerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.CreateRating(
            OwnerUserId,
            "Park:rating-1");
        string geographyCatalogScope =
            PublicCatalogShareSourceScope.CreatePassportGeographyPark(ParkId);
        string ratingsCatalogScope =
            PublicCatalogShareSourceScope.CreatePassportRatingsPark(ParkId);
        Dictionary<string, ShareSourceRevision> available = new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
        {
            [passportScope] = Revision(4),
            [displayNameScope] = Revision(7),
            [avatarScope] = Revision(9),
            [ratingsScope] = Revision(11),
            [geographyCatalogScope] = Revision(3),
            [ratingsCatalogScope] = Revision(3),
        };
        List<IReadOnlyCollection<string>> requestedScopes =
            new List<IReadOnlyCollection<string>>();
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync((IReadOnlyCollection<string> scopes, CancellationToken _) =>
            {
                requestedScopes.Add(scopes.ToArray());
                return scopes.ToDictionary(
                    scope => scope,
                    scope => available[scope],
                    StringComparer.Ordinal);
            });
        PassportProfileSharePublicationSource source = CreateSource(revisions.Object);

        ApplicationResult<long> geographyOnly = await source.GetCurrentSourceVersionAsync(
            Request(sourceScope, CreatePolicy(ShareContentField.GeographicStatistics)),
            CancellationToken.None);
        ApplicationResult<long> withName = await source.GetCurrentSourceVersionAsync(
            Request(sourceScope, CreatePolicy(ShareContentField.PublicDisplayName)),
            CancellationToken.None);
        ApplicationResult<long> withAvatar = await source.GetCurrentSourceVersionAsync(
            Request(sourceScope, CreatePolicy(ShareContentField.Avatar)),
            CancellationToken.None);
        ApplicationResult<long> withRatings = await source.GetCurrentSourceVersionAsync(
            Request(sourceScope, CreatePolicy(ShareContentField.GlobalRatings)),
            CancellationToken.None);

        Assert.Equal(7, geographyOnly.Value);
        Assert.Equal(7, withName.Value);
        Assert.Equal(9, withAvatar.Value);
        Assert.Equal(18, withRatings.Value);
        Assert.DoesNotContain(passportScope, requestedScopes[1]);
        Assert.DoesNotContain(passportScope, requestedScopes[2]);
        Assert.DoesNotContain(geographyCatalogScope, requestedScopes[1]);
        Assert.DoesNotContain(geographyCatalogScope, requestedScopes[2]);
        Assert.DoesNotContain(ratingsCatalogScope, requestedScopes[1]);
        Assert.DoesNotContain(ratingsCatalogScope, requestedScopes[2]);
        IReadOnlyCollection<string> ratingRequest = requestedScopes[3];
        Assert.Contains(ratingsScope, ratingRequest);
        Assert.DoesNotContain(PersonalRankingShareSourceScope.Create(OwnerUserId), ratingRequest);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareOwnedSourceRevisionSnapshotAsync_ShouldRegisterOneCompactMaximumSelectionScope()
    {
        int[] years = Enumerable.Range(1900, 100).ToArray();
        string[] parkIds = Enumerable.Range(1, 250)
            .Select(static index => $"park-{index}")
            .ToArray();
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.EnsureCreatedAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 2),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync((IReadOnlyCollection<string> scopes, CancellationToken _) =>
                scopes.ToDictionary(
                    static scope => scope,
                    static _ => Revision(0),
                    StringComparer.Ordinal));
        Mock<IPassportProfileShareScopeRegistry> registry =
            new Mock<IPassportProfileShareScopeRegistry>(MockBehavior.Strict);
        registry.Setup(value => value.RegisterAsync(
                OwnerUserId,
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<int>>(values => values.Count == 100),
                It.Is<IReadOnlyCollection<string>>(values => values.Count == 250),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PassportProfileSharePublicationSource source = CreateSource(
            revisions.Object,
            registry.Object);

        ApplicationResult<PassportProfileShareSourceRevisionSnapshot> result =
            await source.PrepareOwnedSourceRevisionSnapshotAsync(
                OwnerUserId,
                CreatePolicy(ShareContentField.GeographicStatistics),
                new PassportProfileShareInput(
                    years,
                    parkIds,
                    Array.Empty<string>(),
                    null,
                    ShareVisibility.Unlisted,
                    false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        revisions.Verify(value => value.EnsureCreatedAsync(
            It.IsAny<IReadOnlyCollection<string>>(),
            CancellationToken.None), Times.Once);
        revisions.VerifyAll();
        registry.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldResolveSelectedParksFromPublishedSnapshot()
    {
        SharePublicationId publicationId = SharePublicationId.New();
        string sourceScope = PassportProfileShareSourceScope.Create(OwnerUserId);
        string passportScope = PassportProfileShareSourceScope.CreateFingerprint(
            OwnerUserId,
            new[] { 2026 },
            new[] { ParkId });
        string catalogScope = PublicCatalogShareSourceScope.CreatePassportGeographyPark(ParkId);
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
            Mock.Of<IPassportProfileShareScopeRegistry>(),
            snapshots.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            new SharePublicationSourceVersionRequest(
                sourceScope,
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
        IShareSourceRevisionRepository revisions,
        IPassportProfileShareScopeRegistry? registry = null)
    {
        return new PassportProfileSharePublicationSource(
            revisions,
            registry ?? Mock.Of<IPassportProfileShareScopeRegistry>(),
            Mock.Of<IPassportProfileShareSnapshotRepository>());
    }

    private static SharePublicationSourceVersionRequest Request(
        string sourceScopeKey,
        ShareContentPolicy policy)
    {
        return new SharePublicationSourceVersionRequest(
            sourceScopeKey,
            policy,
            PassportProfile: policy.Includes(ShareContentField.GlobalRatings)
                ? CreateInput("Park:rating-1")
                : CreateInput());
    }

    private static PassportProfileShareInput CreateInput(params string[] ratingKeys)
    {
        return new PassportProfileShareInput(
            new[] { 2026 },
            new[] { ParkId },
            ratingKeys,
            null,
            ShareVisibility.Unlisted,
            false);
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
