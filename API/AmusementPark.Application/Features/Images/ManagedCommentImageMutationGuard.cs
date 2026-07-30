using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images;

internal static class ManagedCommentImageMutationGuard
{
    public static bool IsManagedScope(Image image)
    {
        return IsManagedScope(image.Category, image.OwnerType);
    }

    public static bool IsManagedScope(ImageCategory category, ImageOwnerType? ownerType)
    {
        return category == ImageCategory.Comment
            || ownerType is ImageOwnerType.Comment or ImageOwnerType.CommentDraft;
    }
}
