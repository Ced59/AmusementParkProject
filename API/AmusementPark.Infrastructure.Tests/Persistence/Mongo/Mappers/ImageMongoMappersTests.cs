using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ImageMongoMappersTests
{
    [Fact]
    public void ImageRoundTrip_ShouldPreserveCommentLifecycleFields()
    {
        DateTime cleanupRequestedAtUtc = DateTime.UtcNow.AddMinutes(5);
        DateTime reconcileAfterUtc = DateTime.UtcNow.AddMinutes(2);
        DateTime reservationExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        DateTime reuseExpiresAtUtc = DateTime.UtcNow.AddHours(25);
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            Path = "comment/image-1",
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            DraftOwnerId = "author-1",
            PendingCommentId = "comment-1",
            PendingReservationToken = "reservation-token",
            PendingCommentRevision = 4,
            PendingReservationExpiresAtUtc = reservationExpiresAtUtc,
            AbortedReservationTokens = new List<string> { "aborted-token" },
            ReservationReconcileAfterUtc = reconcileAfterUtc,
            CleanupRequestedAtUtc = cleanupRequestedAtUtc,
            CleanupCommentRevision = 5,
            CommentReuseReservationToken = "reuse-token",
            CommentReuseReconcileAfterUtc = reconcileAfterUtc,
            CommentReuseTargetRevision = 6,
            CommentReuseExpiresAtUtc = reuseExpiresAtUtc,
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        Image result = image.ToDocument().ToDomain();

        Assert.Equal(image.DraftOwnerId, result.DraftOwnerId);
        Assert.Equal(image.PendingCommentId, result.PendingCommentId);
        Assert.Equal(
            image.PendingReservationToken,
            result.PendingReservationToken);
        Assert.Equal(
            image.PendingCommentRevision,
            result.PendingCommentRevision);
        Assert.Equal(
            image.PendingReservationExpiresAtUtc,
            result.PendingReservationExpiresAtUtc);
        Assert.Equal(
            image.AbortedReservationTokens,
            result.AbortedReservationTokens);
        Assert.Equal(
            image.ReservationReconcileAfterUtc,
            result.ReservationReconcileAfterUtc);
        Assert.Equal(image.CleanupRequestedAtUtc, result.CleanupRequestedAtUtc);
        Assert.Equal(
            image.CleanupCommentRevision,
            result.CleanupCommentRevision);
        Assert.Equal(
            image.CommentReuseExpiresAtUtc,
            result.CommentReuseExpiresAtUtc);
    }

    [Fact]
    public void LegacyPendingDraft_ShouldInterpretCleanupTimestampAsReservationDeadline()
    {
        DateTime legacyDeadlineUtc =
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        ImageDocument document = new ImageDocument
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            PendingCommentId = "comment-1",
            PendingReservationToken = "legacy-token",
            CleanupRequestedAt = legacyDeadlineUtc,
            IsPublished = false,
        };

        Image result = document.ToDomain();

        Assert.Equal(legacyDeadlineUtc, result.ReservationReconcileAfterUtc);
        Assert.Null(result.CleanupRequestedAtUtc);
        Assert.Null(result.CleanupCommentRevision);
    }
}
