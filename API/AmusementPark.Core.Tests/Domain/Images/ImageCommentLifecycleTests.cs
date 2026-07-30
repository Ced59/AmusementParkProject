using AmusementPark.Core.Domain.Images;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Images;

public sealed class ImageCommentLifecycleTests
{
    [Fact]
    public void CanBeUsedInComment_WhenPrivateDraftBelongsToActor_ShouldAllowIt()
    {
        Image image = CreateCommentImage(ImageOwnerType.CommentDraft, "author-1", false);

        Assert.True(image.CanBeUsedInComment("author-1", "comment-1"));
        Assert.False(image.CanBeUsedInComment("other-author", "comment-1"));
    }

    [Fact]
    public void CanBeUsedInComment_WhenPublishedImageBelongsToComment_ShouldAllowAnyEditor()
    {
        Image image = CreateCommentImage(ImageOwnerType.Comment, "comment-1", true);

        Assert.True(image.CanBeUsedInComment("admin-editing-comment", "comment-1"));
        Assert.False(image.CanBeUsedInComment("admin-editing-comment", "comment-2"));
    }

    [Fact]
    public void CommentOwnershipRules_WhenCategoryOrPublicationStateIsInvalid_ShouldRejectIt()
    {
        Image image = CreateCommentImage(ImageOwnerType.CommentDraft, "author-1", true);

        Assert.False(image.IsCommentDraftOwnedBy("author-1"));
        Assert.False(image.IsOwnedByComment("author-1"));
    }

    private static Image CreateCommentImage(
        ImageOwnerType ownerType,
        string ownerId,
        bool isPublished)
    {
        return new Image
        {
            Category = ImageCategory.Comment,
            OwnerType = ownerType,
            OwnerId = ownerId,
            IsPublished = isPublished,
        };
    }
}
