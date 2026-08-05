using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkDataEditorTokens;

internal static class ParkDataEditorTokenApplicationErrors
{
    public static ApplicationError InvalidRequest()
    {
        return ApplicationError.Validation(
            "park-data-editor-token.invalid-request",
            "The park data editor token request is invalid.");
    }

    public static ApplicationError UserNotEligible()
    {
        return ApplicationError.Forbidden(
            "park-data-editor-token.user-not-eligible",
            "The user is not eligible for park data editor tokens.");
    }

    public static ApplicationError ActiveTokenLimitReached()
    {
        return ApplicationError.RuleViolation(
            "park-data-editor-token.active-limit-reached",
            "The maximum number of active park data editor tokens has been reached.");
    }

    public static ApplicationError InvalidToken()
    {
        return ApplicationError.Unauthorized(
            "park-data-editor-token.invalid",
            "The park data editor token is invalid.");
    }

    public static ApplicationError TokenNotFound()
    {
        return ApplicationError.NotFound(
            "park-data-editor-token.not-found",
            "The park data editor token was not found or is already revoked.");
    }
}
