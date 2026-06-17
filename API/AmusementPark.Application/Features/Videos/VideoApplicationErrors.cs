using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Videos;

internal static class VideoApplicationErrors
{
    public static ApplicationError VideoNotFound()
    {
        return ApplicationError.NotFound("video.not-found", "Video does not exist.");
    }

    public static ApplicationError VideoUrlInvalid()
    {
        return ApplicationError.Validation("video.url.invalid", "Video URL is invalid or unsupported.");
    }

    public static ApplicationError InvalidOwner()
    {
        return ApplicationError.Validation("video.owner.invalid", "Video owner is invalid.");
    }

    public static ApplicationError VideoTagAlreadyExists(string slug)
    {
        return ApplicationError.Conflict("video-tag.already-exists", $"Video tag '{slug}' already exists.");
    }

    public static ApplicationError VideoTagNotFound()
    {
        return ApplicationError.NotFound("video-tag.not-found", "Video tag does not exist.");
    }

    public static ApplicationError VideoWriteFailed()
    {
        return ApplicationError.Technical("video.write.failed", "Error while writing video metadata.");
    }
}
