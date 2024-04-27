using System.Security.Claims;
using Common.Users;
using Dtos.Users.ChangePassword;
using Dtos.Users.Creating;
using Dtos.Users.ForgotPassword;
using Dtos.Users.ResetPassword;
using Dtos.Users.Roles;
using Dtos.Users.Updating;
using Entities.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces;
using WebAPI.Extensions;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using static Entities.Model.Errors.ErrorCodes;

namespace WebAPI.Controllers
{
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
        //[Authorize(Roles = "Admin,RH")]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserCreateDto user)
        {
            var userCreated = await _usersService.CreateUserAsync(user)!;

            return ApiResponseHandler.HandleResponse(userCreated);
        }

        [HttpGet]
        [Route("by-email")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        public async Task<IActionResult> GetUserByEmailAsync([FromQuery] string email)
        {
            var user = await _usersService.GetUserByEmailAsync(email);

            return ApiResponseHandler.HandleResponse(user);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByIdAsync(string id)
        {
            var user = await _usersService.GetUserByIdAsync(id);
            return ApiResponseHandler.HandleResponse(user);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListUsersAsync([FromQuery] int? page, [FromQuery] int? size)
        {
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "USER")] 
        public async Task<IActionResult> UpdateUserAsync(string id, [FromBody] UserUpdateDto userUpdate)
        {
            var currentUserId = User.GetUserId();

            if (currentUserId != id && !User.IsInRoles(Role.ADMIN, Role.MODERATOR))
            {
                return Forbid();
            }

            var userUpdated = await _usersService.UpdateUser(id, userUpdate);

            return ApiResponseHandler.HandleResponse(userUpdated);
        }


        [HttpPost("change-password")]
        [Authorize(Roles = "USER")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userPasswordChanged = new PasswordChangedDto();
            return ApiResponseHandler.HandleResponse(OneOf<PasswordChangedDto, ErrorDetail>.FromT0(userPasswordChanged));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var passwordReinitialized = new EmailPasswordSendedDto();
            return ApiResponseHandler.HandleResponse(OneOf<EmailPasswordSendedDto, ErrorDetail>.FromT0(passwordReinitialized));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var passwordResetted = new PasswordResetedDto();
            return ApiResponseHandler.HandleResponse(OneOf<PasswordResetedDto, ErrorDetail>.FromT0(passwordResetted));
        }

        [HttpPost("roles/assign/{userId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignRoleAsync(string userId, [FromBody] RoleAssignDto roleAssignDto)
        {
            var roleAssigned = await _usersService.AssignRoleAsync(userId, roleAssignDto);
            return ApiResponseHandler.HandleResponse(roleAssigned);
        }

        [HttpDelete("roles/remove/{userId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RemoveRoleAsync(string userId, [FromBody] RoleRemoveDto roleRemoveDto)
        {
            var roleRemoved = await _usersService.RemoveRoleAsync(userId, roleRemoveDto);
            return ApiResponseHandler.HandleResponse(roleRemoved);
        }

        [HttpPost("lock/{userId}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        public async Task<IActionResult> LockUserAsync(string userId)
        {
            return Ok();
        }

        [HttpPost("unlock/{userId}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        public async Task<IActionResult> UnlockUserAsync(string userId)
        {
            return Ok();
        }
    }

}
