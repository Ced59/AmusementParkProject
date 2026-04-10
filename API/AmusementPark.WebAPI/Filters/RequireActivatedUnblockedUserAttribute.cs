using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.Filters;

/// <summary>
/// Vérifie que l'utilisateur courant est authentifié, activé et non bloqué.
/// </summary>
public sealed class RequireActivatedUnblockedUserAttribute : ActionFilterAttribute, IAsyncActionFilter
{
    public new async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IQueryHandler<GetUserByIdQuery, ApplicationResult<User>>? queryHandler = context.HttpContext.RequestServices.GetService<IQueryHandler<GetUserByIdQuery, ApplicationResult<User>>>();
        string? userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = CreateLegacyError(StatusCodes.Status401Unauthorized, "Need be logged to access at this resource");
            return;
        }

        if (queryHandler is null)
        {
            context.Result = CreateLegacyError(StatusCodes.Status500InternalServerError, "User validation service is unavailable.");
            return;
        }

        ApplicationResult<User> result = await queryHandler.HandleAsync(new GetUserByIdQuery(userId), context.HttpContext.RequestAborted);
        if (!result.IsSuccess || result.Value is null)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = result.Errors.FirstOrDefault()?.Message ?? "User not Exist",
            })
            {
                StatusCode = StatusCodes.Status404NotFound,
            };
            return;
        }

        if (!result.Value.IsActivated)
        {
            context.Result = CreateLegacyError(StatusCodes.Status403Forbidden, "Need be activated to access at this resource");
            return;
        }

        if (result.Value.IsBlocked)
        {
            context.Result = CreateLegacyError(StatusCodes.Status403Forbidden, "User is blocked. You cannot access this resource");
            return;
        }

        await next();
    }

    private static ObjectResult CreateLegacyError(int statusCode, string message)
    {
        return new ObjectResult(new
        {
            StatusCode = statusCode,
            Message = message,
        })
        {
            StatusCode = statusCode,
        };
    }
}
