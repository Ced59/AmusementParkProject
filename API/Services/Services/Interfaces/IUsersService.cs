using Dtos.Pagination;
using Dtos.Users.ChangePassword;
using Dtos.Users.ConfirmEmail;
using Dtos.Users.Creating;
using Dtos.Users.ForgotPassword;
using Dtos.Users.LockUser;
using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Dtos.Users.ResetPassword;
using Dtos.Users.Roles;
using Dtos.Users.Updating;
using Dtos.Users.UserGet;
using Dtos.Users.Users;
using Entities.Model.Users;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Interfaces
{
    public interface IUsersService
    {
        Task<OneOf<UserCreatedDto, ErrorDetail>> CreateUserAsync(UserCreateDto user);

        Task<OneOf<UserLoggedDto, ErrorDetail>> CreateUserByInfosAsync(UserSocialCreate user);

        Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByEmailAsync(string email);

        Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByIdAsync(string id);

        Task<OneOf<UserUpdatedDto, ErrorDetail>> UpdateUserAsync(string id, UserUpdateDto userUpdate);

        Task<OneOf<UserLoggedDto, ErrorDetail>> LoginAsync(UserLoginDto credentials);

        Task<OneOf<UserLoggedDto, ErrorDetail>> LoginExternalAsync(string email);

        Task<OneOf<RefreshTokenResponseDto, ErrorDetail>> RefreshTokenAsync(RefreshTokenRequestDto token);

        Task<OneOf<EmailConfirmedDto, ErrorDetail>> ConfirmEmailAsync(string token);

        Task<OneOf<ConfirmationEmailResentDto, ErrorDetail>> ResendConfirmationEmailAsync(ResendConfirmationEmailDto request);

        Task<OneOf<EmailPasswordSendedDto, ErrorDetail>> ForgotPasswordAsync(ForgotPasswordDto request);

        Task<OneOf<PasswordResetedDto, ErrorDetail>> ResetPasswordAsync(ResetPasswordDto request);

        Task<OneOf<RoleAssignedDto, ErrorDetail>> AssignRoleAsync(string userId, RoleAssignDto roleAssignDto);

        Task<OneOf<RoleRemovedDto, ErrorDetail>> RemoveRoleAsync(string userId, RoleRemoveDto roleToRemove);

        Task<OneOf<UserLockedDto, ErrorDetail>> LockUser(UserToLockDto userToLock);

        Task<OneOf<UserUnlockedDto, ErrorDetail>> UnlockUser(UserToUnlockDto userToUnlock);

        Task<(IEnumerable<UserDto>, PaginationDto)> GetAllUsersPaginatedAsync(int page, int pageSize);

        Task<OneOf<PasswordChangedDto, ErrorDetail>> ChangeUserPassword(string idUser, ChangePasswordDto changePasswordDto, bool changeToSameUser);

        Task<string> DownloadAndSaveUserAvatar(string imageUrl, string userId);
    }
}
