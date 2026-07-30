using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Services;

public sealed class CommentImageReconcilerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DraftCutoffUtc = NowUtc.AddHours(-24);

    [Fact]
    public async Task ReconcileAsync_WhenPendingCreateIsNotVisibleYet_ShouldOnlyReschedule()
    {
        Image pending = CreatePendingDraft(0);
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        SetupPendingReschedule(images, pending);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        images.Verify(value => value.ReleaseCommentDraftReservationForReconciliationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenUpdateRevisionIsStillStale_ShouldOnlyReschedule()
    {
        Image pending = CreatePendingDraft(4);
        Comment stale = CreateComment(3);
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale);
        SetupPendingReschedule(images, pending);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPendingRevisionReferencesImage_ShouldFinalizeExactReservation()
    {
        Image pending = CreatePendingDraft(4);
        Comment committed = CreateComment(4, pending.Id);
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(committed);
        images.Setup(value => value.FinalizeCommentDraftAsync(
                pending.Id,
                "author-1",
                "comment-1",
                "reservation-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = pending.Id,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
                IsPublished = true,
            });

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPendingRevisionExcludesImage_ShouldReleaseForRecheck()
    {
        Image pending = CreatePendingDraft(4);
        Comment committed = CreateComment(4);
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(committed);
        images.Setup(value => value.ReleaseCommentDraftReservationForReconciliationAsync(
                pending.Id,
                "author-1",
                "comment-1",
                "reservation-token",
                pending.ReservationReconcileAfterUtc!.Value,
                It.Is<DateTime>(value => value > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenDeleteCommittedForPendingDraft_ShouldReleasePreservingCleanup()
    {
        Image pending = CreatePendingDraft(3);
        pending.CleanupRequestedAtUtc = NowUtc.AddMinutes(-1);
        pending.CleanupCommentRevision = 4;
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        images.Setup(value => value.ReleaseCommentDraftReservationForReconciliationAsync(
                pending.Id,
                "author-1",
                "comment-1",
                "reservation-token",
                pending.ReservationReconcileAfterUtc!.Value,
                It.Is<DateTime>(value => value > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenUnreservedDraftBecameReferenced_ShouldRecoverIt()
    {
        Image draft = CreateUnreservedDraft(false);
        Mock<IImageRepository> images = CreateCandidateRepository(draft);
        SetupCleanupClaim(images, draft, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetReferencingCommentIdAsync(
                draft.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("comment-1");
        images.Setup(value => value.RecoverClaimedReferencedCommentDraftAsync(
                draft.Id,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                null,
                null,
                It.Is<DateTime>(value => value > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = draft.Id,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
                IsPublished = true,
            });

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenUnreservedRecheckFindsNoReference_ShouldDelayDeletion()
    {
        Image draft = CreateUnreservedDraft(false);
        Mock<IImageRepository> images = CreateCandidateRepository(draft);
        SetupCleanupClaim(images, draft, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetReferencingCommentIdAsync(
                draft.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        images.Setup(value => value.RescheduleClaimedCommentDraftReconciliationAsync(
                draft.Id,
                "author-1",
                It.IsAny<string>(),
                It.Is<DateTime>(value => value > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenExpiredUnreservedDraftHasNoReference_ShouldDeleteIt()
    {
        Image draft = CreateUnreservedDraft(true);
        Mock<IImageRepository> images = CreateCandidateRepository(draft);
        SetupCleanupClaim(images, draft, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetReferencingCommentIdAsync(
                draft.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        Mock<IImageBinaryStorage> storage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(
                draft.Path!,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        images.Setup(value => value.DeleteClaimedCommentImageAsync(
                draft.Id,
                ImageOwnerType.CommentDraft,
                "author-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images, storage)
            .ReconcileAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenCleanupRevisionIsNotCommitted_ShouldReschedule()
    {
        Image published = CreatePublishedCleanup(5);
        Comment stale = CreateComment(4, published.Id);
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale);
        images.Setup(value => value.RescheduleClaimedCommentImageCleanupAsync(
                published.Id,
                ImageOwnerType.Comment,
                "comment-1",
                published.CleanupRequestedAtUtc!.Value,
                5,
                It.IsAny<string>(),
                It.Is<DateTime>(value => value > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenCleanupRevisionStillReferencesImage_ShouldCancel()
    {
        Image published = CreatePublishedCleanup(5);
        Comment committed = CreateComment(5, published.Id);
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(committed);
        images.Setup(value => value.CancelClaimedCommentImageCleanupAsync(
                published.Id,
                ImageOwnerType.Comment,
                "comment-1",
                published.CleanupRequestedAtUtc!.Value,
                5,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images).ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPublishedImageIsOrphaned_ShouldDeleteBinaryBeforeDocument()
    {
        Image published = CreatePublishedCleanup(5);
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments =
            new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        Mock<IImageBinaryStorage> storage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(
                published.Path!,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        images.Setup(value => value.DeleteClaimedCommentImageAsync(
                published.Id,
                ImageOwnerType.Comment,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        int result = await CreateReconciler(comments, images, storage)
            .ReconcileAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenStorageDeletionFails_ShouldKeepClaimForRetry()
    {
        Image published = CreatePublishedCleanup(5);
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments =
            new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        Mock<IImageBinaryStorage> storage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(
                published.Path!,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        int result = await CreateReconciler(comments, images, storage)
            .ReconcileAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                CancellationToken.None);

        Assert.Equal(0, result);
        comments.VerifyAll();
        images.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPublishedCleanupClaimIsLost_ShouldNotReadOrDelete()
    {
        Image published = CreatePublishedCleanup(5);
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, false);
        Mock<ICommentRepository> comments =
            new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);

        int result = await CreateReconciler(comments, images, storage)
            .ReconcileAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                CancellationToken.None);

        Assert.Equal(0, result);
        images.VerifyAll();
        comments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_WhenDraftCleanupClaimIsLost_ShouldNotReadOrDelete()
    {
        Image draft = CreateUnreservedDraft(true);
        Mock<IImageRepository> images = CreateCandidateRepository(draft);
        SetupCleanupClaim(images, draft, false);
        Mock<ICommentRepository> comments =
            new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);

        int result = await CreateReconciler(comments, images, storage)
            .ReconcileAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                CancellationToken.None);

        Assert.Equal(0, result);
        images.VerifyAll();
        comments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    private static CommentImageReconciler CreateReconciler(
        Mock<ICommentRepository> comments,
        Mock<IImageRepository> images,
        Mock<IImageBinaryStorage>? storage = null)
    {
        return new CommentImageReconciler(
            comments.Object,
            images.Object,
            storage?.Object ?? Mock.Of<IImageBinaryStorage>());
    }

    private static Mock<IImageRepository> CreateCandidateRepository(
        Image candidate)
    {
        Mock<IImageRepository> repository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value =>
                value.GetCommentImagesRequiringReconciliationAsync(
                    NowUtc,
                    DraftCutoffUtc,
                    50,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });
        return repository;
    }

    private static void SetupPendingReschedule(
        Mock<IImageRepository> images,
        Image pending)
    {
        images.Setup(value =>
                value.ReschedulePendingCommentDraftReconciliationAsync(
                    pending.Id,
                    "author-1",
                    "comment-1",
                    "reservation-token",
                    pending.ReservationReconcileAfterUtc!.Value,
                    It.Is<DateTime>(date => date > NowUtc),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private static void SetupCleanupClaim(
        Mock<IImageRepository> images,
        Image image,
        bool claimed)
    {
        images.Setup(value => value.TryClaimCommentImageCleanupAsync(
                image.Id,
                image.OwnerType,
                image.OwnerId!,
                NowUtc,
                DraftCutoffUtc,
                It.IsAny<string>(),
                It.Is<DateTime>(claimUntilUtc => claimUntilUtc > NowUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claimed);
    }

    private static Image CreatePendingDraft(long pendingRevision)
    {
        return new Image
        {
            Id = "abcdef0123456789abcdef0123456789",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            DraftOwnerId = "author-1",
            PendingCommentId = "comment-1",
            PendingReservationToken = "reservation-token",
            PendingCommentRevision = pendingRevision,
            ReservationReconcileAfterUtc = NowUtc.AddMinutes(-1),
            CreatedAtUtc = NowUtc.AddHours(-1),
            IsPublished = false,
        };
    }

    private static Image CreateUnreservedDraft(bool expired)
    {
        return new Image
        {
            Id = "abcdef0123456789abcdef0123456789",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            DraftOwnerId = "author-1",
            ReservationReconcileAfterUtc = NowUtc.AddMinutes(-1),
            Path = "comment/draft-1",
            CreatedAtUtc = expired
                ? DraftCutoffUtc.AddMinutes(-1)
                : NowUtc.AddHours(-1),
            IsPublished = false,
        };
    }

    private static Image CreatePublishedCleanup(long cleanupRevision)
    {
        return new Image
        {
            Id = "abcdef0123456789abcdef0123456789",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            DraftOwnerId = "author-1",
            CleanupRequestedAtUtc = NowUtc.AddMinutes(-1),
            CleanupCommentRevision = cleanupRevision,
            Path = "comment/image-1",
            IsPublished = true,
        };
    }

    private static Comment CreateComment(
        long revision,
        params string[] imageIds)
    {
        return new Comment
        {
            Id = "comment-1",
            Revision = revision,
            ImageIds = imageIds.ToList(),
        };
    }
}
