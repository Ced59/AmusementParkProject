using System.ComponentModel.DataAnnotations;
using Common.Users;
using Dtos.Pagination;
using Dtos.Users.ChangePassword;
using Dtos.Users.Creating;
using Dtos.Users.ForgotPassword;
using Dtos.Users.LockUser;
using Dtos.Users.ResetPassword;
using Dtos.Users.Roles;
using Dtos.Users.Updating;
using Dtos.Users.UserGet;
using Dtos.Users.Users;
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
[SwaggerOrder(3)]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService usersService;

    public UsersController(IUsersService usersService)
    {
        this.usersService = usersService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserAsync([FromBody] UserCreateDto user)
    {
        OneOf<UserCreatedDto, ErrorDetail> userCreated = await usersService.CreateUserAsync(user)!;

        return ApiResponseHandler.HandleResponse(userCreated);
    }

    [HttpGet]
    [Route("by-email")]
    public async Task<IActionResult> GetUserByEmailAsync([FromQuery] UserGetByEmailDto userByEmail)
    {
        OneOf<UserGettedDto, ErrorDetail> user = await usersService.GetUserByEmailAsync(userByEmail.Email);

        return ApiResponseHandler.HandleResponse(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserByIdAsync([FromQuery] UserGetByIdDto userById)
    {
        OneOf<UserGettedDto, ErrorDetail> user = await usersService.GetUserByIdAsync(userById.Id);
        return ApiResponseHandler.HandleResponse(user);
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListUsersAsync(
        [FromQuery] [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        int page = 1,
        [FromQuery] [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
        int size = 10)
    {
        (IEnumerable<UserDto> users, PaginationDto pagination) = await usersService.GetAllUsersPaginatedAsync(page, size);
        return ApiResponseHandler.HandleResponse(users, pagination);
    }


    [HttpPut("{id}")]
    [Authorize(Roles = "USER")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> UpdateUserAsync(string id, [FromBody] UserUpdateDto userUpdate)
    {
        string? currentUserId = User.GetUserId();

        if (currentUserId != id && !User.IsInRoles(Role.ADMIN, Role.MODERATOR))
        {
            return ApiResponseHandler.HandleResponse(UserCannotUpdateOtherUser);
        }

        OneOf<UserUpdatedDto, ErrorDetail> userUpdated = await usersService.UpdateUserAsync(id, userUpdate);

        return ApiResponseHandler.HandleResponse(userUpdated);
    }


    [HttpPost("change-password")]
    [Authorize(Roles = "USER")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> ChangePasswordAsync(string idUser, [FromBody] ChangePasswordDto changePasswordDto)
    {
        string? currentUserId = User.GetUserId();
        bool isAdminOrModerator = User.IsInRoles(Role.ADMIN, Role.MODERATOR);

        if (changePasswordDto.NewPassword != changePasswordDto.NewPasswordConfirm)
        {
            return ApiResponseHandler.HandleResponse(PasswordsNotSames);
        }

        if (currentUserId != idUser && !isAdminOrModerator)
        {
            return ApiResponseHandler.HandleResponse(UserCannotChangeOtherPasswordUser);
        }

        bool isSelfChange = currentUserId == idUser;
        OneOf<PasswordChangedDto, ErrorDetail> userPasswordChanged = await usersService.ChangeUserPassword(idUser, changePasswordDto, isSelfChange);
        return ApiResponseHandler.HandleResponse(userPasswordChanged);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        EmailPasswordSendedDto passwordReinitialized = new();
        return ApiResponseHandler.HandleResponse(
            OneOf<EmailPasswordSendedDto, ErrorDetail>.FromT0(passwordReinitialized));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto resetPasswordDto)
    {
        PasswordResetedDto passwordResetted = new();
        return ApiResponseHandler.HandleResponse(OneOf<PasswordResetedDto, ErrorDetail>.FromT0(passwordResetted));
    }

    [HttpPost("roles/assign/{userId}")]
    [Authorize(Roles = "ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> AssignRoleAsync(string userId, [FromBody] RoleAssignDto roleAssignDto)
    {
        OneOf<RoleAssignedDto, ErrorDetail> roleAssigned = await usersService.AssignRoleAsync(userId, roleAssignDto);
        return ApiResponseHandler.HandleResponse(roleAssigned);
    }

    [HttpDelete("roles/remove/{userId}")]
    [Authorize(Roles = "ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> RemoveRoleAsync(string userId, [FromBody] RoleRemoveDto roleRemoveDto)
    {
        OneOf<RoleRemovedDto, ErrorDetail> roleRemoved = await usersService.RemoveRoleAsync(userId, roleRemoveDto);
        return ApiResponseHandler.HandleResponse(roleRemoved);
    }

    [HttpPost("lock")]
    [Authorize(Roles = "MODERATOR,ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> LockUserAsync(UserToLockDto userToLock)
    {
        OneOf<UserLockedDto, ErrorDetail> userLocked = await usersService.LockUser(userToLock);
        return ApiResponseHandler.HandleResponse(userLocked);
    }

    [HttpPost("unlock")]
    [Authorize(Roles = "MODERATOR,ADMIN")]
    [RequireActivatedUnblockedUser]
    public async Task<IActionResult> UnlockUserAsync(UserToUnlockDto userToUnlock)
    {
        OneOf<UserUnlockedDto, ErrorDetail> userUnlocked = await usersService.UnlockUser(userToUnlock);
        return ApiResponseHandler.HandleResponse(userUnlocked);
    }
}