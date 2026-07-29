using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ImageMongoMappersTests
{
    [Fact]
    public void ImageRoundTrip_ShouldPreserveCommentLifecycleFields()
    {
        DateTime cleanupRequestedAtUtc = DateTime.UtcNow.AddMinutes(5);
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            Path = "comment/image-1",
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            DraftOwnerId = "author-1",
            PendingCommentId = "comment-1",
            CleanupRequestedAtUtc = cleanupRequestedAtUtc,
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        Image result = image.ToDocument().ToDomain();

        Assert.Equal(image.DraftOwnerId, result.DraftOwnerId);
        Assert.Equal(image.PendingCommentId, result.PendingCommentId);
        Assert.Equal(image.CleanupRequestedAtUtc, result.CleanupRequestedAtUtc);
    }
}
