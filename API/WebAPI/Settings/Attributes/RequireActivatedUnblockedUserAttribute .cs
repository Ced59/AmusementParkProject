using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Services.Interfaces;
using System.Security.Claims;
using Entities.Model.Errors;
using WebAPI.ResponseHandlers;

namespace WebAPI.Settings.Attributes;

public class RequireActivatedUnblockedUserAttribute : ActionFilterAttribute, IAsyncActionFilter
{
    public new async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userService = context.HttpContext.RequestServices.GetService<IUsersService>();
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Result = ApiResponseHandler.HandleResponse(ErrorCodes.Unauthorized);
            return;
        }

        var result = await userService!.GetUserByIdAsync(userId);

        result.Switch(
            user =>
            {
                if (!(bool)user.IsActivated!)
                {
                    context.Result = ApiResponseHandler.HandleResponse(ErrorCodes.UserNotActivated);
                    return;
                }
                if ((bool)user.IsBlocked!)
                {
                    context.Result = ApiResponseHandler.HandleResponse(ErrorCodes.UserBlocked);
                    return;
                }
            },
            error =>
            {
                context.Result = new ObjectResult(error.Message) { StatusCode = error.StatusCode };
                return;
            }
        );

        if (context.Result == null)
        {
            await next(); 
        }
    }
}
