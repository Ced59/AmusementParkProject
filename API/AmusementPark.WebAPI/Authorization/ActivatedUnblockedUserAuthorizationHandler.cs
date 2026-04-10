using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Handler d'autorisation remplaçant l'ancien filtre métier couplé au pipeline MVC.
/// </summary>
public sealed class ActivatedUnblockedUserAuthorizationHandler : AuthorizationHandler<ActivatedUnblockedUserRequirement>
{
    private readonly IQueryHandler<GetUserByIdQuery, ApplicationResult<User>> getUserByIdQueryHandler;
    private readonly IHttpContextAccessor httpContextAccessor;

    public ActivatedUnblockedUserAuthorizationHandler(
        IQueryHandler<GetUserByIdQuery, ApplicationResult<User>> getUserByIdQueryHandler,
        IHttpContextAccessor httpContextAccessor)
    {
        this.getUserByIdQueryHandler = getUserByIdQueryHandler;
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActivatedUnblockedUserRequirement requirement)
    {
        HttpContext? httpContext = this.httpContextAccessor.HttpContext;
        ClaimsPrincipal principal = context.User;
        bool isAuthenticated = principal.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            context.Fail();
            return;
        }

        string? userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            SetFailure(httpContext, ActivatedUnblockedUserAuthorizationState.MissingUserId);
            context.Fail();
            return;
        }

        CancellationToken cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;
        ApplicationResult<User> result = await this.getUserByIdQueryHandler.HandleAsync(new GetUserByIdQuery(userId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            SetFailure(httpContext, ActivatedUnblockedUserAuthorizationState.UserNotFound);
            context.Fail();
            return;
        }

        if (!result.Value.IsActivated)
        {
            SetFailure(httpContext, ActivatedUnblockedUserAuthorizationState.UserNotActivated);
            context.Fail();
            return;
        }

        if (result.Value.IsBlocked)
        {
            SetFailure(httpContext, ActivatedUnblockedUserAuthorizationState.UserBlocked);
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }

    private static void SetFailure(HttpContext? httpContext, string state)
    {
        if (httpContext is null)
        {
            return;
        }

        httpContext.Items[ActivatedUnblockedUserAuthorizationState.HttpContextItemKey] = state;
    }
}
