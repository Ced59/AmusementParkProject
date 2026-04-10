using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Rejoue les réponses HTTP legacy pour les refus liés à l'état du compte utilisateur.
/// </summary>
public sealed class ActivatedUnblockedUserAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new AuthorizationMiddlewareResultHandler();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        object? failureStateObject = null;
        bool hasFailureState = context.Items.TryGetValue(ActivatedUnblockedUserAuthorizationState.HttpContextItemKey, out failureStateObject);
        if (!hasFailureState || failureStateObject is not string failureState)
        {
            await this.defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (!TryMapFailure(failureState, out int statusCode, out string message))
        {
            await this.defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            StatusCode = statusCode,
            Message = message,
        });
    }

    private static bool TryMapFailure(string failureState, out int statusCode, out string message)
    {
        switch (failureState)
        {
            case ActivatedUnblockedUserAuthorizationState.MissingUserId:
                statusCode = StatusCodes.Status401Unauthorized;
                message = "Need be logged to access at this resource";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserNotFound:
                statusCode = StatusCodes.Status404NotFound;
                message = "User not Exist";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserNotActivated:
                statusCode = StatusCodes.Status403Forbidden;
                message = "Need be activated to access at this resource";
                return true;

            case ActivatedUnblockedUserAuthorizationState.UserBlocked:
                statusCode = StatusCodes.Status403Forbidden;
                message = "User is blocked. You cannot access this resource";
                return true;

            default:
                statusCode = StatusCodes.Status403Forbidden;
                message = string.Empty;
                return false;
        }
    }
}
