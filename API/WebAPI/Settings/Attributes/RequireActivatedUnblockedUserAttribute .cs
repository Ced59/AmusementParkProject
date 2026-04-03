using System.Security.Claims;
using System.Threading.Tasks;
using Dtos.Users.UserGet;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OneOf;
using Services.Interfaces;
using WebAPI.ResponseHandlers;

namespace WebAPI.Settings.Attributes
{
    public class RequireActivatedUnblockedUserAttribute : ActionFilterAttribute, IAsyncActionFilter
    {
        public new async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IUsersService? userService = context.HttpContext.RequestServices.GetService<IUsersService>();
        string? userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Result = ApiResponseHandler.HandleResponse(ErrorCodes.Unauthorized);
            return;
        }

        OneOf<UserGettedDto, ErrorCodes.ErrorDetail> result = await userService!.GetUserByIdAsync(userId);

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
                }
            },
            error => { context.Result = new ObjectResult(error.Message) { StatusCode = error.StatusCode }; }
        );

        if (context.Result == null)
        {
            await next();
        }
    }
    }
}