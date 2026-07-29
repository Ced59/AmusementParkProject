using AmusementPark.Core.Domain.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class CommentMongoMappersTests
{
    [Fact]
    public void CommentRoundTrip_ShouldPreserveReferencedImageIds()
    {
        Comment comment = new Comment
        {
            Id = "comment-1",
            TargetType = CommentTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            AuthorUserId = "author-1",
            ImageIds = new List<string>
            {
                "abcdef0123456789abcdef0123456789",
                "11111111111111111111111111111111",
            },
            ModerationStatus = CommentModerationStatus.Published,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        Comment result = comment.ToDocument().ToDomain();

        Assert.Equal(comment.ImageIds, result.ImageIds);
    }
}
