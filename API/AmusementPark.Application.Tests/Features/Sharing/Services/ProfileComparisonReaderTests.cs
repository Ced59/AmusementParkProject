using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ProfileComparisonReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListForParticipantAsync_ShouldExposeOnlyPublicTokenAndOtherDisplayName()
    {
        ProfileComparison comparison = CreateComparison();
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.ListActiveByParticipantAsync(
                "creator-1",
                ProfileComparisonReader.ManagementListLimit,
                CancellationToken.None))
            .ReturnsAsync(new[] { comparison });
        ProfileComparisonReader reader = CreateReader(repository.Object);

        ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>> result =
            await reader.ListForParticipantAsync("creator-1", CancellationToken.None);

        ProfileComparisonSummaryResult summary = Assert.Single(result.Value!);
        Assert.Equal(comparison.ShareToken.Value, summary.ShareId);
        Assert.Equal("Alex", summary.OtherDisplayName);
        Assert.DoesNotContain(
            summary.GetType().GetProperties(),
            static property => property.Name.EndsWith("UserId", StringComparison.Ordinal)
                || property.Name.EndsWith("PublicationId", StringComparison.Ordinal));
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetSharedAsync_WhenRevoked_ShouldReturnNotFoundWithoutReadingPassports()
    {
        ProfileComparison comparison = CreateComparison();
        comparison.Revoke("acceptor-1", NowUtc.AddMinutes(1));
        Mock<IProfileComparisonRepository> repository =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByShareTokenAsync(
                comparison.ShareToken,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        ProfileComparisonReader reader = CreateReader(repository.Object);

        ApplicationResult<SharedProfileComparisonResult> result = await reader.GetSharedAsync(
            comparison.ShareToken.Value,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "profile-comparison.not-found");
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetSharedAsync_WhenPassportIsWithdrawnDuringRead_ShouldReturnNotFound()
    {
        ProfileComparison comparison = CreateComparison();
        SharePublication creator = CreatePublishedPassport(
            comparison.CreatorPassportPublicationId,
            comparison.CreatorUserId);
        SharePublication acceptor = CreatePublishedPassport(
            comparison.AcceptorPassportPublicationId,
            comparison.AcceptorUserId);
        PassportProfileShareSnapshot creatorSnapshot = CreateSnapshot(creator, "Camille");
        PassportProfileShareSnapshot acceptorSnapshot = CreateSnapshot(acceptor, "Alex");
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        comparisons.Setup(value => value.GetByShareTokenAsync(
                comparison.ShareToken,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.SetupSequence(value => value.GetOwnedAsync(
                creator.Id,
                creator.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(creator)
            .ReturnsAsync((SharePublication?)null);
        publications.Setup(value => value.GetOwnedAsync(
                acceptor.Id,
                acceptor.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(acceptor);
        Mock<IPassportProfileShareSnapshotRepository> snapshots =
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                creator.Id,
                creator.PublicationVersion,
                CancellationToken.None))
            .ReturnsAsync(creatorSnapshot);
        snapshots.Setup(value => value.GetAsync(
                acceptor.Id,
                acceptor.PublicationVersion,
                CancellationToken.None))
            .ReturnsAsync(acceptorSnapshot);
        ProfileComparisonReader reader = CreateReader(
            comparisons.Object,
            publications.Object,
            snapshots.Object);

        ApplicationResult<SharedProfileComparisonResult> result = await reader.GetSharedAsync(
            comparison.ShareToken.Value,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "profile-comparison.not-found");
        comparisons.VerifyAll();
        publications.VerifyAll();
        snapshots.VerifyAll();
    }

    private static ProfileComparisonReader CreateReader(IProfileComparisonRepository repository)
    {
        return CreateReader(
            repository,
            Mock.Of<ISharePublicationRepository>(MockBehavior.Strict),
            Mock.Of<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict));
    }

    private static ProfileComparisonReader CreateReader(
        IProfileComparisonRepository repository,
        ISharePublicationRepository publications,
        IPassportProfileShareSnapshotRepository snapshots)
    {
        ProfileComparisonPassportResolver resolver = new ProfileComparisonPassportResolver(
            publications,
            snapshots);
        return new ProfileComparisonReader(repository, resolver);
    }

    private static SharePublication CreatePublishedPassport(
        SharePublicationId publicationId,
        string ownerUserId)
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[] { ShareContentField.GeographicStatistics });
        SharePublication publication = SharePublication.Create(
            publicationId,
            ownerUserId,
            SharePublicationType.PassportProfile,
            PassportProfileShareSourceScope.Create(ownerUserId),
            policy,
            9,
            NowUtc,
            "fingerprint");
        publication.Publish(
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhA"),
            ShareVisibility.Unlisted,
            9,
            policy,
            0,
            NowUtc,
            "fingerprint");
        return publication;
    }

    private static PassportProfileShareSnapshot CreateSnapshot(
        SharePublication publication,
        string displayName)
    {
        PassportProfileShareInput selection = new PassportProfileShareInput(
            Array.Empty<int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            true);
        PassportProfileSharePreviewResult content = new PassportProfileSharePreviewResult(
            displayName,
            null,
            null,
            ShareVisibility.Unlisted,
            true,
            1,
            1,
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
        return new PassportProfileShareSnapshot(
            publication.Id,
            publication.PublicationVersion,
            publication.Version,
            publication.SourceVersion,
            publication.ContentPolicy.SchemaVersion,
            publication.ContentPolicy.DatePrecision,
            publication.ContentPolicy.IncludedFields,
            publication.ContentFingerprint,
            selection,
            content,
            NowUtc);
    }

    private static ProfileComparison CreateComparison()
    {
        return ProfileComparison.Create(
            ProfileComparisonId.Parse("comparison-1"),
            ProfileComparisonInvitationId.Parse("invitation-1"),
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            "creator-1",
            "acceptor-1",
            SharePublicationId.Parse("creator-passport"),
            1,
            SharePublicationId.Parse("acceptor-passport"),
            1,
            new ProfileComparisonCalculation(
                "Camille",
                "Alex",
                new[] { ProfileComparisonCategory.VisitedParks },
                Array.Empty<ProfileComparisonParkResult>(),
                Array.Empty<ProfileComparisonRatingResult>(),
                Array.Empty<ProfileComparisonYearResult>(),
                Array.Empty<ProfileComparisonMissedItemResult>(),
                0,
                ProfileComparisonCalculator.MinimumRatingsForCorrelation,
                null,
                false,
                ProfileComparisonCalculator.CalculationVersion),
            NowUtc);
    }
}
