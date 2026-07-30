using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Services;

public sealed class CommentImageManagerTests
{
    private const string ImageId = "abcdef0123456789abcdef0123456789";

    [Fact]
    public async Task PublishForCommentAsync_WhenDraftBelongsToActor_ShouldReserveItPrivately()
    {
        Image draft = CreateDraft("author-1");
        string? capturedReservationToken = null;
        long? capturedPendingRevision = null;
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { ImageId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                string _,
                string _,
                string reservationToken,
                long pendingRevision,
                DateTime _,
                CancellationToken _) =>
            {
                capturedReservationToken = reservationToken;
                capturedPendingRevision = pendingRevision;
            })
            .ReturnsAsync(new Image
            {
                Id = ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                PendingCommentId = "comment-1",
                IsPublished = false,
            });
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult<CommentImageReservationBatch> result =
            await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None,
            7);

        Assert.True(result.IsSuccess);
        CommentImageReservationBatch batch =
            Assert.IsType<CommentImageReservationBatch>(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(capturedReservationToken));
        Assert.Equal(7, capturedPendingRevision);
        Assert.Equal(capturedReservationToken, batch.ReservationToken);
        Assert.Equal(7, batch.PendingCommentRevision);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenDraftBelongsToAnotherActor_ShouldRejectIt()
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateDraft("other-author") });
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None,
            1);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenImageIsAlreadyOnSameComment_ShouldKeepIt()
    {
        Image published = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { published });
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                ImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PublishedCommentImageReusePreparation.Prepared);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult result = await manager.PublishForCommentAsync(
            "admin-editing-another-comment",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None,
            1);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenCleanupClaimWins_ShouldRejectPublishedImageReuse()
    {
        Image published = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            CleanupRequestedAtUtc = DateTime.UtcNow,
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { published });
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                ImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PublishedCommentImageReusePreparation.Rejected);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None,
            1);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenLaterPublishedPreparationThrows_ShouldRestoreEarlierAndCurrentCleanup()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Image firstPublished = CreatePublished(ImageId);
        Image secondPublished = CreatePublished(secondImageId);
        string? capturedReservationToken = null;
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstPublished, secondPublished });
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                ImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                1,
                It.IsAny<CancellationToken>()))
            .Callback((
                string _imageId,
                string _commentId,
                string reservationToken,
                DateTime _reconcileAfterUtc,
                long _targetCommentRevision,
                CancellationToken _cancellationToken) =>
                capturedReservationToken = reservationToken)
            .ReturnsAsync(PublishedCommentImageReusePreparation.PreparedAndCleanupCleared);
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                secondImageId,
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                It.IsAny<DateTime>(),
                1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Preparation failed."));
        repository
            .Setup(value => value.ReleasePublishedCommentImageReuseAsync(
                ImageId,
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                It.IsAny<DateTime>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        repository
            .Setup(value => value.ReleasePublishedCommentImageReuseAsync(
                secondImageId,
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                It.IsAny<DateTime>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.PublishForCommentAsync(
                "author-1",
                "comment-1",
                new[] { ImageId, secondImageId },
                CancellationToken.None,
                1));

        Assert.Equal("Preparation failed.", exception.Message);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenLaterPublishedPreparationIsCanceled_ShouldRestoreEarlierAndCurrentCleanup()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Image firstPublished = CreatePublished(ImageId);
        Image secondPublished = CreatePublished(secondImageId);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                cancellation.Token))
            .ReturnsAsync(new[] { firstPublished, secondPublished });
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                ImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<long>(),
                cancellation.Token))
            .ReturnsAsync(PublishedCommentImageReusePreparation.PreparedAndCleanupCleared);
        repository
            .Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                secondImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<long>(),
                cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        repository
            .Setup(value => value.ReleasePublishedCommentImageReuseAsync(
                ImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        repository
            .Setup(value => value.ReleasePublishedCommentImageReuseAsync(
                secondImageId,
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => manager.PublishForCommentAsync(
                "author-1",
                "comment-1",
                new[] { ImageId, secondImageId },
                cancellation.Token,
                1));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenMoreThanLimit_ShouldRejectBeforeReadingRepository()
    {
        CommentImageManager manager = new CommentImageManager(Mock.Of<IImageRepository>());
        string[] ids = Enumerable.Range(0, CommentImageManager.MaximumImagesPerComment + 1)
            .Select(static index => index.ToString("x32"))
            .ToArray();

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            ids,
            CancellationToken.None,
            1);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.images.too-many");
    }

    [Fact]
    public async Task RequestRemovedCleanupAsync_ShouldPersistCleanupBeforeDeletion()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.RequestCommentImagesCleanupAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { ImageId, secondImageId })),
                "comment-1",
                8,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        await manager.RequestRemovedCleanupAsync(
            "comment-1",
            8,
            new[] { ImageId, secondImageId },
            CancellationToken.None);

        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenSecondReservationFails_ShouldReleaseFirstReservation()
    {
        const string secondImageId = "11111111111111111111111111111111";
        string? capturedReservationToken = null;
        Image firstDraft = CreateDraft("author-1");
        Image secondDraft = new Image
        {
            Id = secondImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            IsPublished = false,
        };
        Image firstReserved = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            PendingCommentId = "comment-1",
            IsPublished = false,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstDraft, secondDraft });
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                string _,
                string _,
                string reservationToken,
                long _,
                DateTime _,
                CancellationToken _) =>
                capturedReservationToken = reservationToken)
            .ReturnsAsync(firstReserved);
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                secondImageId,
                "author-1",
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        repository
            .Setup(value => value.ReleaseCommentDraftReservationAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult<CommentImageReservationBatch> result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId, secondImageId },
            CancellationToken.None,
            1);

        Assert.False(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenSecondReservationThrows_ShouldReleaseFirstReservation()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Image firstDraft = CreateDraft("author-1");
        Image secondDraft = new Image
        {
            Id = secondImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            IsPublished = false,
        };
        Image firstReserved = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            PendingCommentId = "comment-1",
            IsPublished = false,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstDraft, secondDraft });
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstReserved);
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                secondImageId,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("reservation failed"));
        repository
            .Setup(value => value.ReleaseCommentDraftReservationAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId, secondImageId },
            CancellationToken.None,
            1));

        repository.VerifyAll();
    }

    [Fact]
    public async Task ReleaseReservationsForCommentAsync_WhenRollbackPartlyFails_ShouldContinueBestEffort()
    {
        const string secondImageId = "11111111111111111111111111111111";
        const string thirdImageId = "22222222222222222222222222222222";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.ReleaseCommentDraftReservationAsync(
                ImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ReturnsAsync(false);
        repository.Setup(value => value.ReleaseCommentDraftReservationAsync(
                secondImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("release failed"));
        repository.Setup(value => value.ReleaseCommentDraftReservationAsync(
                thirdImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        IReadOnlyCollection<string> failedImageIds =
            await manager.ReleaseReservationsForCommentAsync(
                "author-1",
                "comment-1",
                new CommentImageReservationBatch(
                    new[] { ImageId, secondImageId, thirdImageId },
                    "reservation-token",
                    Array.Empty<string>()));

        Assert.Equal(new[] { ImageId, secondImageId }, failedImageIds);
        repository.VerifyAll();
    }

    [Fact]
    public async Task FinalizeForCommentAsync_WhenRepositoryCannotFinalize_ShouldReturnFailedImageIds()
    {
        const string failedImageId = "11111111111111111111111111111111";
        const string exceptionImageId = "22222222222222222222222222222222";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.FinalizeCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ReturnsAsync(new Image { Id = ImageId });
        repository
            .Setup(value => value.FinalizeCommentDraftAsync(
                failedImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ReturnsAsync((Image?)null);
        repository
            .Setup(value => value.FinalizeCommentDraftAsync(
                exceptionImageId,
                "author-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("finalization failed"));
        CommentImageManager manager = new CommentImageManager(repository.Object);

        IReadOnlyCollection<string> failedImageIds = await manager.FinalizeForCommentAsync(
            "author-1",
            "comment-1",
            new CommentImageReservationBatch(
                new[] { ImageId, failedImageId, exceptionImageId },
                "reservation-token",
                Array.Empty<string>()));

        Assert.Equal(new[] { failedImageId, exceptionImageId }, failedImageIds);
        repository.VerifyAll();
    }

    [Fact]
    public async Task FinalizeForCommentAsync_WhenPublishedReuseWasPrepared_ShouldFinalizeOwningToken()
    {
        Mock<IImageRepository> repository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.FinalizePublishedCommentImageReuseAsync(
                ImageId,
                "comment-1",
                "reservation-token",
                CancellationToken.None))
            .ReturnsAsync(true);
        CommentImageManager manager =
            new CommentImageManager(repository.Object);

        IReadOnlyCollection<string> failedImageIds =
            await manager.FinalizeForCommentAsync(
                "author-1",
                "comment-1",
                new CommentImageReservationBatch(
                    Array.Empty<string>(),
                    "reservation-token",
                    new[] { ImageId }));

        Assert.Empty(failedImageIds);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RestorePreparedCleanupForCommentAsync_WhenMongoFails_ShouldLeaveDurableMarkerForWorker()
    {
        Mock<IImageRepository> repository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.ReleasePublishedCommentImageReuseAsync(
                ImageId,
                "comment-1",
                "reservation-token",
                It.IsAny<DateTime>(),
                0,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable."));
        CommentImageManager manager =
            new CommentImageManager(repository.Object);

        IReadOnlyCollection<string> failedImageIds =
            await manager.RestorePreparedCleanupForCommentAsync(
                "comment-1",
                new CommentImageReservationBatch(
                    Array.Empty<string>(),
                    "reservation-token",
                    new[] { ImageId }));

        Assert.Equal(new[] { ImageId }, failedImageIds);
        repository.VerifyAll();
    }

    [Fact]
    public async Task DeleteOwnedDraftAsync_WhenDraftBelongsToAnotherActor_ShouldRejectWithoutDeletion()
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(ImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDraft("other-author"));
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult result = await manager.DeleteOwnedDraftAsync(
            "author-1",
            ImageId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    [Fact]
    public async Task DeleteOwnedDraftAsync_WhenDraftIsReserved_ShouldRejectWithoutSchedulingDeletion()
    {
        Image draft = CreateDraft("author-1");
        draft.PendingCommentId = "comment-1";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(ImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        CommentImageManager manager = new CommentImageManager(repository.Object);

        ApplicationResult result = await manager.DeleteOwnedDraftAsync(
            "author-1",
            ImageId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    private static Image CreateDraft(string ownerId)
    {
        return new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = ownerId,
            IsPublished = false,
        };
    }

    private static Image CreatePublished(string imageId)
    {
        return new Image
        {
            Id = imageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            CleanupRequestedAtUtc = DateTime.UtcNow,
            IsPublished = true,
        };
    }
}
