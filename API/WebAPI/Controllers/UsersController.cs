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
using WebAPI.ResponseHandlers;
using static Entities.Model.Errors.ErrorCodes;

namespace WebAPI.Controllers
{
    [ApiController]
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
            var userCreated = await _usersService!.CreateUserAsync(user);

            return ApiResponseHandler.HandleResponse(userCreated);
        }

        [HttpGet]
        [Route("by-email")]
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
        public async Task<IActionResult> UpdateUserAsync(string id, [FromBody] UserUpdateDto userUpdate)
        {
            var userUpdated = await _usersService.UpdateUser(id, userUpdate);

            return ApiResponseHandler.HandleResponse(userUpdated);
        }

        [HttpPost("change-password")]
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
        public async Task<IActionResult> AssignRoleAsync(string userId, [FromBody] RoleAssignDto roleAssignDto)
        {
            var roleAssigned = new RoleAssignedDto();
            return ApiResponseHandler.HandleResponse(OneOf<RoleAssignedDto, ErrorDetail>.FromT0(roleAssigned));
        }

        [HttpDelete("roles/remove/{userId}")]
        public async Task<IActionResult> RemoveRoleAsync(string userId, [FromBody] RoleRemoveDto roleRemoveDto)
        {
            var roleRemoved = new RoleRemovedDto();
            return ApiResponseHandler.HandleResponse(OneOf<RoleRemovedDto, ErrorDetail>.FromT0(roleRemoved));
        }

        [HttpPost("lock/{userId}")]
        public async Task<IActionResult> LockUserAsync(string userId)
        {
            return Ok();
        }

        [HttpPost("unlock/{userId}")]
        public async Task<IActionResult> UnlockUserAsync(string userId)
        {
            return Ok();
        }
    }

}
