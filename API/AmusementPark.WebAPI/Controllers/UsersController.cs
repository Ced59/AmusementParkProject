using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Users migrée en phase 10.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly ICommandHandler<RegisterLocalUserCommand, ApplicationResult<User>> registerLocalUserCommandHandler;
    private readonly IQueryHandler<GetUserByEmailQuery, ApplicationResult<User>> getUserByEmailQueryHandler;
    private readonly IQueryHandler<GetUserByIdQuery, ApplicationResult<User>> getUserByIdQueryHandler;
    private readonly IQueryHandler<GetUsersPageQuery, ApplicationResult<PagedResult<User>>> getUsersPageQueryHandler;
    private readonly ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>> updateUserProfileCommandHandler;
    private readonly ICommandHandler<ChangePasswordCommand, ApplicationResult> changePasswordCommandHandler;
    private readonly ICommandHandler<ConfirmEmailCommand, ApplicationResult<User>> confirmEmailCommandHandler;
    private readonly ICommandHandler<ResendConfirmationEmailCommand, ApplicationResult> resendConfirmationEmailCommandHandler;
    private readonly ICommandHandler<ForgotPasswordCommand, ApplicationResult> forgotPasswordCommandHandler;
    private readonly ICommandHandler<ResetPasswordCommand, ApplicationResult> resetPasswordCommandHandler;
    private readonly ICommandHandler<AssignRoleCommand, ApplicationResult<User>> assignRoleCommandHandler;
    private readonly ICommandHandler<RemoveRoleCommand, ApplicationResult<User>> removeRoleCommandHandler;
    private readonly ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserCommandHandler;
    private readonly ICommandHandler<UnlockUserCommand, ApplicationResult<User>> unlockUserCommandHandler;

    public UsersController(
        ICommandHandler<RegisterLocalUserCommand, ApplicationResult<User>> registerLocalUserCommandHandler,
        IQueryHandler<GetUserByEmailQuery, ApplicationResult<User>> getUserByEmailQueryHandler,
        IQueryHandler<GetUserByIdQuery, ApplicationResult<User>> getUserByIdQueryHandler,
        IQueryHandler<GetUsersPageQuery, ApplicationResult<PagedResult<User>>> getUsersPageQueryHandler,
        ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>> updateUserProfileCommandHandler,
        ICommandHandler<ChangePasswordCommand, ApplicationResult> changePasswordCommandHandler,
        ICommandHandler<ConfirmEmailCommand, ApplicationResult<User>> confirmEmailCommandHandler,
        ICommandHandler<ResendConfirmationEmailCommand, ApplicationResult> resendConfirmationEmailCommandHandler,
        ICommandHandler<ForgotPasswordCommand, ApplicationResult> forgotPasswordCommandHandler,
        ICommandHandler<ResetPasswordCommand, ApplicationResult> resetPasswordCommandHandler,
        ICommandHandler<AssignRoleCommand, ApplicationResult<User>> assignRoleCommandHandler,
        ICommandHandler<RemoveRoleCommand, ApplicationResult<User>> removeRoleCommandHandler,
        ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserCommandHandler,
        ICommandHandler<UnlockUserCommand, ApplicationResult<User>> unlockUserCommandHandler)
    {
        this.registerLocalUserCommandHandler = registerLocalUserCommandHandler;
        this.getUserByEmailQueryHandler = getUserByEmailQueryHandler;
        this.getUserByIdQueryHandler = getUserByIdQueryHandler;
        this.getUsersPageQueryHandler = getUsersPageQueryHandler;
        this.updateUserProfileCommandHandler = updateUserProfileCommandHandler;
        this.changePasswordCommandHandler = changePasswordCommandHandler;
        this.confirmEmailCommandHandler = confirmEmailCommandHandler;
        this.resendConfirmationEmailCommandHandler = resendConfirmationEmailCommandHandler;
        this.forgotPasswordCommandHandler = forgotPasswordCommandHandler;
        this.resetPasswordCommandHandler = resetPasswordCommandHandler;
        this.assignRoleCommandHandler = assignRoleCommandHandler;
        this.removeRoleCommandHandler = removeRoleCommandHandler;
        this.lockUserCommandHandler = lockUserCommandHandler;
        this.unlockUserCommandHandler = unlockUserCommandHandler;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRegistration)]
    [ProducesResponseType(typeof(UserCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUserAsync([FromBody] UserCreateDto user, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.registerLocalUserCommandHandler.HandleAsync(
            new RegisterLocalUserCommand(user.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToCreatedDto());
    }

    [HttpGet("by-email")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserGettedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserByEmailAsync([FromQuery] UserGetByEmailDto userByEmail, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.getUserByEmailQueryHandler.HandleAsync(new GetUserByEmailQuery(userByEmail.Email), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToGettedDto());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserGettedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        string? currentUserId = this.User.GetUserId();
        if (currentUserId != id && !this.User.IsInRoles(UserRoleDto.ADMIN, UserRoleDto.MODERATOR))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status403Forbidden, "You cannot access another user account.", "user.access-denied");
        }

        ApplicationResult<User> result = await this.getUserByIdQueryHandler.HandleAsync(new GetUserByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToGettedDto());
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PagedResponseDto<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsersAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<User>> result = await this.getUsersPageQueryHandler.HandleAsync(
            new GetUsersPageQuery(pagination.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static user => user.ToListDto()));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserUpdatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserAsync([FromRoute] string id, [FromBody] UserUpdateDto userUpdate, CancellationToken cancellationToken = default)
    {
        string? currentUserId = this.User.GetUserId();
        if (currentUserId != id && !this.User.IsInRoles(UserRoleDto.ADMIN, UserRoleDto.MODERATOR))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status403Forbidden, "You cannot update another user account.", "user.update-denied");
        }

        ApplicationResult<User> result = await this.updateUserProfileCommandHandler.HandleAsync(
            new UpdateUserProfileCommand(id, userUpdate.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToUpdatedDto());
    }

    [HttpPost("change-password")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PasswordChangedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePasswordAsync([FromQuery] string idUser, [FromBody] ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(changePasswordDto.NewPassword, changePasswordDto.NewPasswordConfirm, StringComparison.Ordinal))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Passwords do not match.", "password.confirmation-mismatch");
        }

        string? currentUserId = this.User.GetUserId();
        bool isAdminOrModerator = this.User.IsInRoles(UserRoleDto.ADMIN, UserRoleDto.MODERATOR);
        if (currentUserId != idUser && !isAdminOrModerator)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status403Forbidden, "You cannot update another user password.", "password.update-denied");
        }

        bool isSelfChange = currentUserId == idUser;
        ApplicationResult result = await this.changePasswordCommandHandler.HandleAsync(
            new ChangePasswordCommand(idUser, changePasswordDto.ToApplication(), isSelfChange),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new PasswordChangedDto
        {
            Message = $"Password of user {idUser} successfully changed",
        });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthEmailChallenge)]
    [ProducesResponseType(typeof(EmailConfirmedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequestDto confirmEmailRequestDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.confirmEmailCommandHandler.HandleAsync(
            new ConfirmEmailCommand(confirmEmailRequestDto.Token),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new EmailConfirmedDto
        {
            Message = "Account successfully activated.",
        });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthEmailChallenge)]
    [ProducesResponseType(typeof(ConfirmationEmailResentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationAsync([FromBody] ResendConfirmationEmailDto resendConfirmationEmailDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.resendConfirmationEmailCommandHandler.HandleAsync(
            new ResendConfirmationEmailCommand(resendConfirmationEmailDto.Email),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new ConfirmationEmailResentDto
        {
            Message = "If the account exists and is not yet activated, a new confirmation email has been sent.",
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthEmailChallenge)]
    [ProducesResponseType(typeof(EmailPasswordSendedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto forgotPasswordDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.forgotPasswordCommandHandler.HandleAsync(
            new ForgotPasswordCommand(forgotPasswordDto.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new EmailPasswordSendedDto
        {
            Message = "If the account exists, a password reset email has been sent.",
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthPasswordReset)]
    [ProducesResponseType(typeof(PasswordResetedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.resetPasswordCommandHandler.HandleAsync(
            new ResetPasswordCommand(resetPasswordDto.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new PasswordResetedDto
        {
            Message = "Password has been reset successfully.",
        });
    }

    [HttpPost("roles/assign/{userId}")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(RoleAssignedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRoleAsync([FromRoute] string userId, [FromBody] RoleAssignDto roleAssignDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.assignRoleCommandHandler.HandleAsync(
            new AssignRoleCommand(userId, roleAssignDto.Role.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToAssignedDto());
    }

    [HttpDelete("roles/remove/{userId}")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(RoleRemovedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveRoleAsync([FromRoute] string userId, [FromBody] RoleRemoveDto roleRemoveDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.removeRoleCommandHandler.HandleAsync(
            new RemoveRoleCommand(userId, roleRemoveDto.Role.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToRemovedDto());
    }

    [HttpPost("lock")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserLockedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LockUserAsync([FromBody] UserToLockDto userToLock, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.lockUserCommandHandler.HandleAsync(new LockUserCommand(userToLock.IdUser), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToLockedDto());
    }

    [HttpPost("unlock")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserUnlockedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlockUserAsync([FromBody] UserToUnlockDto userToUnlock, CancellationToken cancellationToken = default)
    {
        ApplicationResult<User> result = await this.unlockUserCommandHandler.HandleAsync(new UnlockUserCommand(userToUnlock.IdUser), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToUnlockedDto());
    }

}
