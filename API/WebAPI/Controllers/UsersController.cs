using System.ComponentModel.DataAnnotations;
using Common.Users;
using Dtos.Users.ChangePassword;
using Dtos.Users.Creating;
using Dtos.Users.ForgotPassword;
using Dtos.Users.LockUser;
using Dtos.Users.ResetPassword;
using Dtos.Users.Roles;
using Dtos.Users.Updating;
using Dtos.Users.UserGet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces;
using WebAPI.Extensions;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using static Entities.Model.Errors.ErrorCodes;

namespace WebAPI.Controllers;

[ApiController]
[SwaggerOrder(2)]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserAsync([FromBody] UserCreateDto user)
    {
        var userCreated = await _usersService.CreateUserAsync(user)!;

        return ApiResponseHandler.HandleResponse(userCreated);
    }

    [HttpGet]
    [Route("by-email")]
    public async Task<IActionResult> GetUserByEmailAsync([FromQuery] UserGetByEmailDto userByEmail)
    {
        var user = await _usersService.GetUserByEmailAsync(userByEmail.Email);

        return ApiResponseHandler.HandleResponse(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserByIdAsync([FromQuery] UserGetByIdDto userById)
    {
        var user = await _usersService.GetUserByIdAsync(userById.Id);
        return ApiResponseHandler.HandleResponse(user);
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListUsersAsync(
        [FromQuery] [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        int page = 1,
        [FromQuery] [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
        int size = 10)
    {
        var (users, pagination) = await _usersService.GetAllUsersPaginatedAsync(page, size);
        return ApiResponseHandler.HandleResponse(users, pagination);
    }


    [HttpPut("{id}")]
    [Authorize(Roles = "USER")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> UpdateUserAsync(string id, [FromBody] UserUpdateDto userUpdate)
    {
        var currentUserId = User.GetUserId();

        if (currentUserId != id && !User.IsInRoles(Role.ADMIN, Role.MODERATOR))
            return ApiResponseHandler.HandleResponse(UserCannotUpdateOtherUser);

        var userUpdated = await _usersService.UpdateUser(id, userUpdate);

        return ApiResponseHandler.HandleResponse(userUpdated);
    }


    [HttpPost("change-password")]
    [Authorize(Roles = "USER")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> ChangePasswordAsync(string idUser, [FromBody] ChangePasswordDto changePasswordDto)
    {
        var currentUserId = User.GetUserId();
        var isAdminOrModerator = User.IsInRoles(Role.ADMIN, Role.MODERATOR);

        if (changePasswordDto.NewPassword != changePasswordDto.NewPasswordConfirm)
        {
            return ApiResponseHandler.HandleResponse(PasswordsNotSames);
        }

        if (currentUserId != idUser && !isAdminOrModerator)
        {
            return ApiResponseHandler.HandleResponse(UserCannotChangeOtherPasswordUser);
        }

        var isSelfChange = currentUserId == idUser;
        var userPasswordChanged = await _usersService.ChangeUserPassword(idUser, changePasswordDto, isSelfChange);
        return ApiResponseHandler.HandleResponse(userPasswordChanged);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var passwordReinitialized = new EmailPasswordSendedDto();
        return ApiResponseHandler.HandleResponse(
            OneOf<EmailPasswordSendedDto, ErrorDetail>.FromT0(passwordReinitialized));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var passwordResetted = new PasswordResetedDto();
        return ApiResponseHandler.HandleResponse(OneOf<PasswordResetedDto, ErrorDetail>.FromT0(passwordResetted));
    }

    [HttpPost("roles/assign/{userId}")]
    [Authorize(Roles = "ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> AssignRoleAsync(string userId, [FromBody] RoleAssignDto roleAssignDto)
    {
        var roleAssigned = await _usersService.AssignRoleAsync(userId, roleAssignDto);
        return ApiResponseHandler.HandleResponse(roleAssigned);
    }

    [HttpDelete("roles/remove/{userId}")]
    [Authorize(Roles = "ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> RemoveRoleAsync(string userId, [FromBody] RoleRemoveDto roleRemoveDto)
    {
        var roleRemoved = await _usersService.RemoveRoleAsync(userId, roleRemoveDto);
        return ApiResponseHandler.HandleResponse(roleRemoved);
    }

    [HttpPost("lock")]
    [Authorize(Roles = "MODERATOR,ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> LockUserAsync(UserToLockDto userToLock)
    {
        var userLocked = await _usersService.LockUser(userToLock);
        return ApiResponseHandler.HandleResponse(userLocked);
    }

    [HttpPost("unlock")]
    [Authorize(Roles = "MODERATOR,ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> UnlockUserAsync(UserToUnlockDto userToUnlock)
    {
        var userUnlocked = await _usersService.UnlockUser(userToUnlock);
        return ApiResponseHandler.HandleResponse(userUnlocked);
    }
}