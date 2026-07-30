using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Ports;
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
    public async Task ReconcileAsync_WhenPendingDraftIsReferenced_ShouldFinalizeIt()
    {
        Image pending = CreatePendingDraft();
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(pending.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            Mock.Of<IImageBinaryStorage>());

        int result = await reconciler.ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPendingDraftIsNotReferenced_ShouldReleaseIt()
    {
        Image pending = CreatePendingDraft();
        Mock<IImageRepository> images = CreateCandidateRepository(pending);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(pending.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        images.Setup(value => value.ReleaseCommentDraftReservationAsync(
                pending.Id,
                "author-1",
                "comment-1",
                "reservation-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            Mock.Of<IImageBinaryStorage>());

        int result = await reconciler.ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenPublishedImageIsStillReferenced_ShouldCancelCleanup()
    {
        Image published = CreatePublishedCleanup();
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(published.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        images.Setup(value => value.CancelClaimedCommentImageCleanupAsync(
                published.Id,
                ImageOwnerType.Comment,
                "comment-1",
                published.CleanupRequestedAtUtc!.Value,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            Mock.Of<IImageBinaryStorage>());

        int result = await reconciler.ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(1, result);
        comments.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenStorageDeletionFails_ShouldKeepTombstoneForRetry()
    {
        Image published = CreatePublishedCleanup();
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(published.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(published.Path!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            storage.Object);

        int result = await reconciler.ReconcileAsync(
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
    public async Task ReconcileAsync_WhenPublishedImageIsOrphaned_ShouldDeleteBinaryBeforeDocument()
    {
        Image published = CreatePublishedCleanup();
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, true);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(published.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(published.Path!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        images.Setup(value => value.DeleteClaimedCommentImageAsync(
                published.Id,
                ImageOwnerType.Comment,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            storage.Object);

        int result = await reconciler.ReconcileAsync(
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
    public async Task ReconcileAsync_WhenCleanupClaimLosesToReuse_ShouldNotDeleteBinary()
    {
        Image published = CreatePublishedCleanup();
        Mock<IImageRepository> images = CreateCandidateRepository(published);
        SetupCleanupClaim(images, published, false);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            storage.Object);

        int result = await reconciler.ReconcileAsync(
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
    public async Task ReconcileAsync_WhenDraftClaimLosesToReservation_ShouldNotDeleteBinary()
    {
        Image draft = CreateExpiredDraft();
        Mock<IImageRepository> images = CreateCandidateRepository(draft);
        SetupCleanupClaim(images, draft, false);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        CommentImageReconciler reconciler = new CommentImageReconciler(
            comments.Object,
            images.Object,
            storage.Object);

        int result = await reconciler.ReconcileAsync(
            NowUtc,
            DraftCutoffUtc,
            50,
            CancellationToken.None);

        Assert.Equal(0, result);
        images.VerifyAll();
        comments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    private static Mock<IImageRepository> CreateCandidateRepository(Image candidate)
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetCommentImagesRequiringReconciliationAsync(
                NowUtc,
                DraftCutoffUtc,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });
        return repository;
    }

    private static Image CreatePendingDraft()
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
            CleanupRequestedAtUtc = NowUtc.AddMinutes(-1),
            IsPublished = false,
        };
    }

    private static Image CreateExpiredDraft()
    {
        Image draft = CreatePendingDraft();
        draft.PendingCommentId = null;
        draft.CleanupRequestedAtUtc = null;
        draft.Path = "comment/draft-1";
        draft.CreatedAtUtc = DraftCutoffUtc.AddMinutes(-1);
        return draft;
    }

    private static Image CreatePublishedCleanup()
    {
        return new Image
        {
            Id = "abcdef0123456789abcdef0123456789",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            DraftOwnerId = "author-1",
            CleanupRequestedAtUtc = NowUtc.AddMinutes(-1),
            Path = "comment/image-1",
            IsPublished = true,
        };
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
}
