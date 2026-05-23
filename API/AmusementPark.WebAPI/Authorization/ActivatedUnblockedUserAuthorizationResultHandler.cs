using System.Threading.Tasks;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Retourne les refus d'autorisation au format RFC 7807 unique de l'API.
/// </summary>
public sealed class ActivatedUnblockedUserAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        object? failureStateObject = null;
        bool hasFailureState = context.Items.TryGetValue(ActivatedUnblockedUserAuthorizationState.HttpContextItemKey, out failureStateObject);
        if (hasFailureState && failureStateObject is string failureState && TryMapFailure(failureState, out int statusCode, out string detail, out string errorCode))
        {
            await WriteAuthorizationProblemAsync(context, statusCode, detail, errorCode);
            return;
        }

        if (authorizeResult.Challenged)
        {
            context.Response.Headers.TryAdd("WWW-Authenticate", "Bearer");
            await WriteAuthorizationProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ApiProblemDetailsFactory.GetDefaultDetail(StatusCodes.Status401Unauthorized),
                "authentication.required");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteAuthorizationProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                ApiProblemDetailsFactory.GetDefaultDetail(StatusCodes.Status403Forbidden),
                "authorization.forbidden");
            return;
        }

        await next(context);
    }

    private static Task WriteAuthorizationProblemAsync(HttpContext context, int statusCode, string detail, string errorCode)
    {
        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            context,
            statusCode,
            ApiProblemDetailsFactory.GetDefaultTitle(statusCode),
            detail,
            errorCode);

        return ApiProblemDetailsFactory.WriteAsync(context, problemDetails);
    }

    private static bool TryMapFailure(string failureState, out int statusCode, out string detail, out string errorCode)
    {
        switch (failureState)
        {
            case ActivatedUnblockedUserAuthorizationState.MissingUserId:
                statusCode = StatusCodes.Status401Unauthorized;
                detail = "You must be authenticated to access this resource.";
                errorCode = "authentication.required";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserNotFound:
                statusCode = StatusCodes.Status404NotFound;
                detail = "The authenticated user no longer exists.";
                errorCode = "user.not-found";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserNotActivated:
                statusCode = StatusCodes.Status403Forbidden;
                detail = "The authenticated account must be activated before accessing this resource.";
                errorCode = "user.not-activated";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserBlocked:
                statusCode = StatusCodes.Status403Forbidden;
                detail = "The authenticated account is blocked.";
                errorCode = "user.blocked";
                return true;

            default:
                statusCode = StatusCodes.Status403Forbidden;
                detail = string.Empty;
                errorCode = string.Empty;
                return false;
        }
    }
}
