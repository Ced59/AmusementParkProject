using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using System.Text.Json;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ShareAccountDeletionServiceTests
{
    private const string OwnerId = "owner-1";
    private const string PublicationToken =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string ComparisonToken =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhA";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeleteAsync_ShouldBlackoutEveryPublicEntryBeforePurgingDocuments()
    {
        SharePublication publication = CreatePublishedPublication();
        ProfileComparison comparison = CreateComparison();
        bool publicationRevoked = false;
        bool comparisonRevoked = false;
        bool invitationExpired = false;
        bool cachesInvalidated = false;

        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.ListOwnedAsync(OwnerId, CancellationToken.None))
            .ReturnsAsync(new[] { publication });
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Status == SharePublicationStatus.Revoked
                    && candidate.ShareToken == null),
                1,
                CancellationToken.None))
            .Callback(() => publicationRevoked = true)
            .ReturnsAsync(SharePublicationWriteOutcome.Success);

        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        comparisons.Setup(value => value.ListActiveByParticipantAsync(
                OwnerId,
                null,
                100,
                CancellationToken.None))
            .ReturnsAsync(new[] { comparison });
        comparisons.Setup(value => value.ReplaceAsync(
                It.Is<ProfileComparison>(candidate =>
                    !candidate.IsActive && candidate.RevokedByUserId == OwnerId),
                0,
                CancellationToken.None))
            .Callback(() => comparisonRevoked = true)
            .ReturnsAsync(ProfileComparisonWriteOutcome.Success);

        Mock<IProfileComparisonInvitationRepository> invitations =
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict);
        invitations.Setup(value => value.DeletePendingCreatedByAsync(
                OwnerId,
                CancellationToken.None))
            .Callback(() =>
            {
                Assert.True(publicationRevoked);
                Assert.True(comparisonRevoked);
                invitationExpired = true;
            })
            .ReturnsAsync(2L);

        Mock<IShareSocialImageCacheInvalidator> socialImages =
            new Mock<IShareSocialImageCacheInvalidator>(MockBehavior.Strict);
        socialImages.Setup(value => value.Invalidate(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.Count == 2
                    && ids.Contains(PublicationToken)
                    && ids.Contains(ComparisonToken))))
            .Callback(() => Assert.True(invitationExpired));
        Mock<ISsrPageCacheInvalidator> ssr =
            new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssr.Setup(value => value.InvalidateAllAsync(CancellationToken.None))
            .Callback(() => cachesInvalidated = true)
            .Returns(Task.CompletedTask);

        Mock<IShareAccountDeletionStore> deletionStore =
            new Mock<IShareAccountDeletionStore>(MockBehavior.Strict);
        deletionStore.Setup(value => value.PurgeAsync(OwnerId, CancellationToken.None))
            .Callback(() =>
            {
                Assert.True(publicationRevoked);
                Assert.True(comparisonRevoked);
                Assert.True(invitationExpired);
                Assert.True(cachesInvalidated);
            })
            .ReturnsAsync(8L);

        Mock<IDurableBackgroundJobRepository> jobs = CreateJobRepository();
        ShareAccountDeletionService service = new ShareAccountDeletionService(
            publications.Object,
            invitations.Object,
            comparisons.Object,
            deletionStore.Object,
            new SharePublicationCacheInvalidationScheduler(jobs.Object),
            socialImages.Object,
            ssr.Object,
            new SharePublicationFixedTimeProvider(NowUtc));

        ApplicationResult<ShareAccountDeletionResult> result = await service.DeleteAsync(
            " owner-1 ",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.RevokedPublicationCount);
        Assert.Equal(2, result.Value.ExpiredInvitationCount);
        Assert.Equal(1, result.Value.RevokedComparisonCount);
        Assert.Equal(8, result.Value.PurgedDocumentCount);
        jobs.Verify(value => value.EnqueueExactAsync(
            It.IsAny<EnqueueExactBackgroundJobRequest>(),
            CancellationToken.None), Times.Exactly(2));
        jobs.Verify(value => value.EnqueueExactAsync(
            It.Is<EnqueueExactBackgroundJobRequest>(request =>
                IsInvalidationFor(
                    request,
                    "publication-1",
                    SharePublicationType.PassportProfile,
                    PublicationToken)),
            CancellationToken.None), Times.Once);
        jobs.Verify(value => value.EnqueueExactAsync(
            It.Is<EnqueueExactBackgroundJobRequest>(request =>
                IsInvalidationFor(
                    request,
                    "comparison-1",
                    SharePublicationType.ProfileComparison,
                    ComparisonToken)),
            CancellationToken.None), Times.Once);
        publications.VerifyAll();
        comparisons.VerifyAll();
        invitations.VerifyAll();
        socialImages.VerifyAll();
        ssr.VerifyAll();
        deletionStore.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenPublicationKeepsChanging_ShouldNeverPurge()
    {
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.ListOwnedAsync(OwnerId, CancellationToken.None))
            .ReturnsAsync(new[] { CreatePublishedPublication() });
        publications.Setup(value => value.ReplaceAsync(
                It.IsAny<SharePublication>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Conflict);
        publications.Setup(value => value.GetOwnedAsync(
                SharePublicationId.Parse("publication-1"),
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(() => CreatePublishedPublication());

        Mock<IShareAccountDeletionStore> deletionStore =
            new Mock<IShareAccountDeletionStore>(MockBehavior.Strict);
        ShareAccountDeletionService service = new ShareAccountDeletionService(
            publications.Object,
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict).Object,
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict).Object,
            deletionStore.Object,
            new SharePublicationCacheInvalidationScheduler(CreateJobRepository().Object),
            new Mock<IShareSocialImageCacheInvalidator>(MockBehavior.Strict).Object,
            new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict).Object,
            new SharePublicationFixedTimeProvider(NowUtc));

        ApplicationResult<ShareAccountDeletionResult> result = await service.DeleteAsync(
            OwnerId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "sharing.account-deletion-conflict");
        deletionStore.Verify(
            value => value.PurgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        publications.VerifyAll();
    }

    private static Mock<IDurableBackgroundJobRepository> CreateJobRepository()
    {
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        return jobs;
    }

    private static bool IsInvalidationFor(
        EnqueueExactBackgroundJobRequest request,
        string recordId,
        SharePublicationType publicationType,
        string shareId)
    {
        SharePublicationCacheInvalidationJobPayload? payload =
            request.Payload.Deserialize<SharePublicationCacheInvalidationJobPayload>();
        return payload is not null
            && payload.PublicationId == recordId
            && payload.PublicationType == publicationType
            && payload.ShareIds.SequenceEqual(new[] { shareId });
    }

    private static SharePublication CreatePublishedPublication()
    {
        return SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            OwnerId,
            SharePublicationType.PassportProfile,
            PassportProfileShareSourceScope.Create(OwnerId),
            ShareToken.Parse(PublicationToken),
            SharePublicationStatus.Published,
            ShareVisibility.Public,
            ShareContentPolicy.CreatePrivateDefault(SharePublicationType.PassportProfile),
            4,
            1,
            1,
            NowUtc.AddDays(-1),
            null,
            NowUtc.AddDays(-2),
            NowUtc.AddDays(-1));
    }

    private static ProfileComparison CreateComparison()
    {
        return ProfileComparison.Create(
            ProfileComparisonId.Parse("comparison-1"),
            ProfileComparisonInvitationId.Parse("invitation-1"),
            ShareToken.Parse(ComparisonToken),
            OwnerId,
            "other-user",
            SharePublicationId.Parse("publication-1"),
            1,
            SharePublicationId.Parse("publication-2"),
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
            NowUtc.AddDays(-1));
    }
}
