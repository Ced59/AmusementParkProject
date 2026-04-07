using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Images;

/// <summary>
/// Erreurs applicatives dédiées à la feature Images avec messages alignés sur le legacy.
/// </summary>
internal static class ImageApplicationErrors
{
    public static ApplicationError NoImageFileProvided()
    {
        return ApplicationError.NotFound("image.file.missing", "No image filename provided.");
    }

    public static ApplicationError NoImageCategoryProvided()
    {
        return ApplicationError.NotFound("image.category.missing", "No image category provided.");
    }

    public static ApplicationError ImageProcessingFailed()
    {
        return ApplicationError.Technical("image.processing.failed", "Image processing Internal Server Error");
    }

    public static ApplicationError ImageNotExists()
    {
        return ApplicationError.NotFound("image.not-found", "Image does not exist.");
    }

    public static ApplicationError ImageNotLinkedToOwner()
    {
        return ApplicationError.Validation("image.owner.missing", "Image is not linked to any owner.");
    }

    public static ApplicationError ErrorUpdatingImageLink()
    {
        return ApplicationError.Technical("image.link.update.failed", "Error while updating image link.");
    }

    public static ApplicationError ErrorSettingCurrentImage()
    {
        return ApplicationError.Technical("image.current.failed", "Error while setting current image.");
    }

    public static ApplicationError ErrorDeletingImage()
    {
        return ApplicationError.Technical("image.delete.failed", "Error while deleting image.");
    }

    public static ApplicationError ImageTagAlreadyExists(string slug)
    {
        return ApplicationError.Conflict("image-tag.already-exists", $"Image tag '{slug}' already exists.");
    }

    public static ApplicationError ImageTagNotExists()
    {
        return ApplicationError.NotFound("image-tag.not-found", "Image tag does not exist.");
    }

    public static ApplicationError InvalidOwner()
    {
        return ApplicationError.Validation("image.owner.invalid", "Image owner is invalid.");
    }
}
