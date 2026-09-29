using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.LiveData;

public static class LiveDataApplicationErrors
{
    public static ApplicationError PublicReadDisabled()
    {
        return ApplicationError.NotFound(
            "live-data.public-read.disabled",
            "Public live data is not enabled.");
    }

    public static ApplicationError InvalidMapping(string? message = null)
    {
        return ApplicationError.Validation(
            "live-data.mapping.invalid",
            message ?? "The live target mapping is invalid.");
    }

    public static ApplicationError MappingNotFound()
    {
        return ApplicationError.NotFound(
            "live-data.mapping.not-found",
            "The live target mapping was not found.");
    }

    public static ApplicationError TargetNotFound()
    {
        return ApplicationError.NotFound(
            "live-data.target.not-found",
            "The selected internal live target was not found or is inconsistent with its park.");
    }

    public static ApplicationError ParentMappingNotVerified()
    {
        return ApplicationError.RuleViolation(
            "live-data.mapping.parent-not-verified",
            "A park item can only be verified when its external parent park is verified against the same internal park.");
    }

    public static ApplicationError Conflict(int currentRevision)
    {
        return ApplicationError.Conflict(
            "live-data.mapping.revision-conflict",
            "The live target mapping changed during this edit. Reload its latest revision.",
            currentRevision);
    }

    public static ApplicationError AlreadyExists()
    {
        return ApplicationError.Conflict(
            "live-data.mapping.already-exists",
            "A mapping already exists for this source target.");
    }

    public static ApplicationError InvalidTransition(string? message = null)
    {
        return ApplicationError.RuleViolation(
            "live-data.mapping.invalid-transition",
            message ?? "The requested live mapping transition is not allowed.");
    }

    public static ApplicationError InvalidSearch()
    {
        return ApplicationError.Validation(
            "live-data.mapping.search.invalid",
            "The live mapping search is invalid.");
    }

    public static ApplicationError InvalidReplay()
    {
        return ApplicationError.Validation(
            "live-data.quality.replay.invalid",
            "The live quality replay request is invalid.");
    }

    public static ApplicationError InvalidOperationalControl(string? message = null)
    {
        return ApplicationError.Validation(
            "live-data.operational-control.invalid",
            message ?? "The live operational control is invalid.");
    }

    public static ApplicationError OperationalControlConflict(int currentRevision)
    {
        return ApplicationError.Conflict(
            "live-data.operational-control.revision-conflict",
            "The live operational control changed during this edit. Reload its latest revision.",
            currentRevision);
    }

    public static ApplicationError OperationsUnavailable()
    {
        return ApplicationError.NotFound(
            "live-data.operations.unavailable",
            "No live data pilot is configured for operations.");
    }
}
