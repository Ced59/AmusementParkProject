using System.Security.Claims;
using System.Text.RegularExpressions;
using Common.Users;
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
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Authentication;
using Services.Interfaces.Images;
using Services.Interfaces.Settings;
using Services.Security;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations
{
    public class UsersService : IUsersService
    {
        private readonly IJwtSettings jwtSettings;
        private readonly ILocalAuthenticationSettings localAuthenticationSettings;
        private readonly IUserQueryHandler userQueryHandler;
        private readonly IUserAvatarService userAvatarService;
        private readonly ILocalAccountTokenService localAccountTokenService;
        private readonly ILocalAccountEmailService localAccountEmailService;

        public UsersService(
            IUserQueryHandler userQueryHandler,
            IJwtSettings jwtSettings,
            IUserAvatarService userAvatarService,
            ILocalAuthenticationSettings localAuthenticationSettings,
            ILocalAccountTokenService localAccountTokenService,
            ILocalAccountEmailService localAccountEmailService)
        {
            this.userQueryHandler = userQueryHandler;
            this.jwtSettings = jwtSettings;
            this.userAvatarService = userAvatarService;
            this.localAuthenticationSettings = localAuthenticationSettings;
            this.localAccountTokenService = localAccountTokenService;
            this.localAccountEmailService = localAccountEmailService;
        }

        public async Task<OneOf<UserCreatedDto, ErrorDetail>> CreateUserAsync(UserCreateDto user)
        {
            if (user.Password != user.VerifyPassword)
            {
                return PasswordsNotSames;
            }

            string? normalizedEmail = NormalizeEmail(user.Email);
            if (!IsValidEmail(normalizedEmail))
            {
                return InvalidEmailAddress;
            }

            if (await userQueryHandler.ExistsByEmailAsync(normalizedEmail))
            {
                return UserAlreadyExists;
            }

            if (string.IsNullOrWhiteSpace(user.Password) || !IsValidPassword(user.Password))
            {
                return InvalidPassword;
            }

            DateTime now = DateTime.UtcNow;
            string confirmationToken = localAccountTokenService.GenerateToken();

            User userToCreate = new()
            {
                Email = normalizedEmail,
                CreatedAt = now,
                UpdatedAt = now,
                PreferredLanguage = string.IsNullOrWhiteSpace(user.PreferredLanguage)
                    ? "EN"
                    : user.PreferredLanguage.Trim().ToUpperInvariant(),
                IsActivated = false,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.USER
                },
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password),
                LastLogin = now,
                LastActivity = now,
                EmailConfirmationTokenHash = localAccountTokenService.ComputeHash(confirmationToken),
                EmailConfirmationTokenExpiresAt = now.AddHours(localAuthenticationSettings.EmailConfirmationTokenExpirationHours),
                EmailConfirmationSentAt = now
            };

            User? userCreated = await userQueryHandler.CreateUserAsync(userToCreate);
            if (userCreated is null)
            {
                return UserUpdateFailed;
            }

            await localAccountEmailService.SendEmailConfirmationAsync(userCreated, confirmationToken);

            return MapToCreatedDto(userCreated);
        }

        public async Task<OneOf<UserLoggedDto, ErrorDetail>> CreateUserByInfosAsync(UserSocialCreate user)
        {
            User userToCreate = new()
            {
                Email = NormalizeEmail(user.Email),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PreferredLanguage = "EN",
                AvatarUrl = user.AvatarUrl,
                IsActivated = true,
                IsBlocked = false,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = new List<Role>
                {
                    Role.USER
                },
                HashedPassword = string.Empty
            };

            User? userCreated = await userQueryHandler.CreateUserAsync(userToCreate);
            if (userCreated is null)
            {
                return LoginFailed;
            }

            User? userLogin = await userQueryHandler.GetUserByEmailAsync(userCreated.Email);
            if (userLogin is null)
            {
                return LoginFailed;
            }

            string token = JwtHelper.GenerateToken(userLogin, jwtSettings);
            await userQueryHandler.UpdateLastLoginAndActivityAsync(userLogin.Id);
            return new UserLoggedDto { Token = token };
        }

        public async Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByEmailAsync(string email)
        {
            User? user = await userQueryHandler.GetUserByEmailAsync(NormalizeEmail(email));
            if (user is null)
            {
                return UserNotExists;
            }

            return MapToGetDto(user);
        }

        public async Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByIdAsync(string id)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(id);
            if (user is null)
            {
                return UserNotExists;
            }

            return MapToGetDto(user);
        }

        public async Task<OneOf<UserUpdatedDto, ErrorDetail>> UpdateUserAsync(string id, UserUpdateDto userUpdate)
        {
            string? currentEmail = NormalizeEmail(userUpdate.Email);
            string? newEmail = NormalizeEmail(userUpdate.NewEmail);

            if (!IsValidEmail(currentEmail) || (newEmail is not null && !IsValidEmail(newEmail)))
            {
                return InvalidEmailAddress;
            }

            User? userToUpdate = await userQueryHandler.GetUserByIdAsync(id);
            if (userToUpdate is null)
            {
                return UserNotExists;
            }

            string? confirmationToken = null;
            bool emailChanged = !string.IsNullOrWhiteSpace(newEmail) && !string.Equals(userToUpdate.Email, newEmail, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                User? existingUser = await userQueryHandler.GetUserByEmailAsync(newEmail);
                if (existingUser is not null && existingUser.Id != userToUpdate.Id)
                {
                    return UserUpdateFailed;
                }

                userToUpdate.Email = newEmail;
                userToUpdate.IsActivated = false;
                PrepareEmailConfirmation(userToUpdate, out confirmationToken);
            }

            userToUpdate.LastName = userUpdate.LastName;
            userToUpdate.FirstName = userUpdate.FirstName;
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            userToUpdate.PreferredLanguage = userUpdate.PreferredLanguage;
            userToUpdate.LastActivity = DateTime.UtcNow;
            if (userUpdate.AvatarUrl is not null)
            {
                userToUpdate.AvatarUrl = userUpdate.AvatarUrl;
            }

            User? userUpdated = await userQueryHandler.UpdateUserAsync(userToUpdate);
            if (userUpdated is null)
            {
                return UserUpdateFailed;
            }

            if (!string.IsNullOrWhiteSpace(confirmationToken))
            {
                await localAccountEmailService.SendEmailConfirmationAsync(userUpdated, confirmationToken);
            }

            return MapToUpdatedDto(userUpdated);
        }

        public async Task<OneOf<UserLoggedDto, ErrorDetail>> LoginAsync(UserLoginDto credentials)
        {
            string? normalizedEmail = NormalizeEmail(credentials.Email);
            User? user = await userQueryHandler.GetUserByEmailAsync(normalizedEmail);
            if (user is null)
            {
                return LoginFailed;
            }

            if (string.IsNullOrWhiteSpace(user.HashedPassword))
            {
                return LocalLoginNotAvailable;
            }

            if (!BCrypt.Net.BCrypt.Verify(credentials.Password, user.HashedPassword))
            {
                return LoginFailed;
            }

            if (!user.IsActivated)
            {
                return UserNotActivated;
            }

            if (user.IsBlocked)
            {
                return UserBlocked;
            }

            string token = JwtHelper.GenerateToken(user, jwtSettings);
            await userQueryHandler.UpdateLastLoginAndActivityAsync(user.Id);
            return new UserLoggedDto { Token = token };
        }

        public async Task<OneOf<UserLoggedDto, ErrorDetail>> LoginExternalAsync(string email)
        {
            User? user = await userQueryHandler.GetUserByEmailAsync(NormalizeEmail(email));
            if (user is null)
            {
                return LoginFailed;
            }

            string token = JwtHelper.GenerateToken(user, jwtSettings);
            await userQueryHandler.UpdateLastLoginAndActivityAsync(user.Id);
            return new UserLoggedDto { Token = token };
        }

        public async Task<OneOf<RefreshTokenResponseDto, ErrorDetail>> RefreshTokenAsync(RefreshTokenRequestDto token)
        {
            JwtHelper.ValidationResult result = JwtHelper.ValidateToken(token.RefreshToken, false, jwtSettings);
            if (!result.IsValid || result.Token is null)
            {
                return TokenRefreshFailed;
            }

            Claim? idUser = result.Token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
            if (idUser is null)
            {
                return TokenRefreshFailed;
            }

            User? user = await userQueryHandler.GetUserByIdAsync(idUser.Value);
            if (user is null)
            {
                return TokenRefreshFailed;
            }

            DateTime expireAt = user.LastActivity + TimeSpan.FromMinutes(jwtSettings.TokenRefreshLimitMinutes);
            if (DateTime.UtcNow > expireAt)
            {
                return TokenRefreshFailedInactivity;
            }

            return new RefreshTokenResponseDto
            {
                RefreshToken = JwtHelper.GenerateToken(user, jwtSettings)
            };
        }

        public async Task<OneOf<EmailConfirmedDto, ErrorDetail>> ConfirmEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return EmailConfirmationTokenInvalid;
            }

            string tokenHash = localAccountTokenService.ComputeHash(token);
            User? user = await userQueryHandler.GetUserByEmailConfirmationTokenHashAsync(tokenHash);
            if (user is null)
            {
                return EmailConfirmationTokenInvalid;
            }

            if (user.IsActivated)
            {
                return AccountAlreadyActivated;
            }

            if (!user.EmailConfirmationTokenExpiresAt.HasValue || user.EmailConfirmationTokenExpiresAt.Value < DateTime.UtcNow)
            {
                return EmailConfirmationTokenExpired;
            }

            user.IsActivated = true;
            user.UpdatedAt = DateTime.UtcNow;
            ClearEmailConfirmation(user);

            User? updatedUser = await userQueryHandler.UpdateUserAsync(user);
            if (updatedUser is null)
            {
                return UserUpdateFailed;
            }

            return new EmailConfirmedDto
            {
                Message = "Account successfully activated."
            };
        }

        public async Task<OneOf<ConfirmationEmailResentDto, ErrorDetail>> ResendConfirmationEmailAsync(ResendConfirmationEmailDto request)
        {
            string? normalizedEmail = NormalizeEmail(request.Email);
            if (!IsValidEmail(normalizedEmail))
            {
                return InvalidEmailAddress;
            }

            User? user = await userQueryHandler.GetUserByEmailAsync(normalizedEmail);
            if (user is not null && !user.IsActivated)
            {
                string confirmationToken;
                PrepareEmailConfirmation(user, out confirmationToken);
                User? updatedUser = await userQueryHandler.UpdateUserAsync(user);
                if (updatedUser is null)
                {
                    return ConfirmationEmailResendFailed;
                }

                await localAccountEmailService.SendEmailConfirmationAsync(updatedUser, confirmationToken);
            }

            return new ConfirmationEmailResentDto
            {
                Message = "If the account exists and is not yet activated, a new confirmation email has been sent."
            };
        }

        public async Task<OneOf<EmailPasswordSendedDto, ErrorDetail>> ForgotPasswordAsync(ForgotPasswordDto request)
        {
            string? normalizedEmail = NormalizeEmail(request.Email);
            if (!IsValidEmail(normalizedEmail))
            {
                return InvalidEmailAddress;
            }

            User? user = await userQueryHandler.GetUserByEmailAsync(normalizedEmail);
            if (user is not null && user.IsActivated && !string.IsNullOrWhiteSpace(user.HashedPassword))
            {
                string resetToken;
                PreparePasswordReset(user, out resetToken);
                User? updatedUser = await userQueryHandler.UpdateUserAsync(user);
                if (updatedUser is null)
                {
                    return PasswordResetEmailSendFailed;
                }

                await localAccountEmailService.SendPasswordResetAsync(updatedUser, resetToken);
            }

            return new EmailPasswordSendedDto
            {
                Message = "If the account exists, a password reset email has been sent."
            };
        }

        public async Task<OneOf<PasswordResetedDto, ErrorDetail>> ResetPasswordAsync(ResetPasswordDto request)
        {
            if (request.NewPassword != request.NewPasswordConfirm)
            {
                return PasswordsNotSames;
            }

            if (!IsValidPassword(request.NewPassword))
            {
                return InvalidPassword;
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return PasswordResetTokenInvalid;
            }

            string tokenHash = localAccountTokenService.ComputeHash(request.Token);
            User? user = await userQueryHandler.GetUserByPasswordResetTokenHashAsync(tokenHash);
            if (user is null)
            {
                return PasswordResetTokenInvalid;
            }

            if (!user.PasswordResetTokenExpiresAt.HasValue || user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
            {
                return PasswordResetTokenExpired;
            }

            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.LastActivity = DateTime.UtcNow;
            ClearPasswordReset(user);

            User? updatedUser = await userQueryHandler.UpdateUserAsync(user);
            if (updatedUser is null)
            {
                return PasswordResetFailed;
            }

            return new PasswordResetedDto
            {
                Message = "Password has been reset successfully."
            };
        }

        public async Task<OneOf<RoleAssignedDto, ErrorDetail>> AssignRoleAsync(string userId, RoleAssignDto roleToAssign)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(userId);
            if (user is null)
            {
                return UserNotExists;
            }

            if (user.Roles.Contains(roleToAssign.Role))
            {
                return RoleAlreadyAssigned;
            }

            User? updatedUser = await userQueryHandler.AssignRoleAsync(userId, roleToAssign.Role);
            if (updatedUser is null)
            {
                return AssignRoleFailed;
            }

            return new RoleAssignedDto
            {
                UserId = updatedUser.Id,
                Roles = updatedUser.Roles
            };
        }

        public async Task<OneOf<RoleRemovedDto, ErrorDetail>> RemoveRoleAsync(string userId, RoleRemoveDto roleToRemove)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(userId);
            if (user is null)
            {
                return UserNotExists;
            }

            if (!user.Roles.Contains(roleToRemove.Role))
            {
                return RoleNotAssigned;
            }

            User? updatedUser = await userQueryHandler.RemoveRoleAsync(userId, roleToRemove.Role);
            if (updatedUser is null)
            {
                return RemoveRoleFailed;
            }

            return new RoleRemovedDto
            {
                UserId = updatedUser.Id,
                Roles = updatedUser.Roles
            };
        }

        public async Task<OneOf<UserLockedDto, ErrorDetail>> LockUser(UserToLockDto userToLock)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(userToLock.IdUser);
            if (user is null)
            {
                return UserNotExists;
            }

            User? userLocked = await userQueryHandler.LockUser(userToLock.IdUser);
            if (userLocked is null)
            {
                return CannotLockUser;
            }

            return new UserLockedDto
            {
                FirstName = userLocked.FirstName,
                LastName = userLocked.LastName,
                UserId = userLocked.Id
            };
        }

        public async Task<OneOf<UserUnlockedDto, ErrorDetail>> UnlockUser(UserToUnlockDto userToUnlock)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(userToUnlock.IdUser);
            if (user is null)
            {
                return UserNotExists;
            }

            User? userUnlocked = await userQueryHandler.UnlockUser(userToUnlock.IdUser);
            if (userUnlocked is null)
            {
                return CannotUnlockUser;
            }

            return new UserUnlockedDto
            {
                FirstName = userUnlocked.FirstName,
                LastName = userUnlocked.LastName,
                UserId = userUnlocked.Id
            };
        }

        public async Task<(IEnumerable<UserDto>, PaginationDto)> GetAllUsersPaginatedAsync(int page, int pageSize)
        {
            long totalItems = await userQueryHandler.GetTotalUsersCountAsync();
            IEnumerable<User> users = await userQueryHandler.GetUsersPaginatedAsync(page, pageSize);
            PaginationDto pagination = PaginationDto.Create((int)totalItems, page, pageSize);

            IEnumerable<UserDto> usersDto = users.Select(user => new UserDto
            {
                CreatedAt = user.CreatedAt,
                IsActivated = user.IsActivated,
                UpdatedAt = user.UpdatedAt,
                LastActivity = user.LastActivity,
                LastName = user.LastName,
                FirstName = user.FirstName,
                Email = user.Email,
                Roles = user.Roles,
                Id = user.Id,
                IsBlocked = user.IsBlocked,
                LastLogin = user.LastLogin,
                PreferredLanguage = user.PreferredLanguage,
                AvatarUrl = user.AvatarUrl
            });

            return (usersDto, pagination);
        }

        public async Task<OneOf<PasswordChangedDto, ErrorDetail>> ChangeUserPassword(string idUser, ChangePasswordDto changePasswordDto, bool isSelfChanged)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(idUser);
            if (user is null)
            {
                return UserNotExists;
            }

            if (!IsValidPassword(changePasswordDto.NewPassword))
            {
                return InvalidPassword;
            }

            bool hasLocalPassword = !string.IsNullOrWhiteSpace(user.HashedPassword);
            if (isSelfChanged && hasLocalPassword && !BCrypt.Net.BCrypt.Verify(changePasswordDto.ActualPassword, user.HashedPassword))
            {
                return IncorrectPassword;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            bool changePasswordOk = await userQueryHandler.ChangePassword(idUser, hashedPassword);
            if (!changePasswordOk)
            {
                return ChangePasswordFailed;
            }

            return new PasswordChangedDto
            {
                Message = $"Password of user {idUser} successfully changed"
            };
        }

        public async Task<string> DownloadAndSaveUserAvatar(string imageUrl, string userId)
        {
            return await userAvatarService.ImportExternalAvatarAsync(
                imageUrl,
                userId,
                ExternalLoginProvider.Google);
        }

        private static UserCreatedDto MapToCreatedDto(User user)
        {
            return new UserCreatedDto
            {
                CreatedAt = user.CreatedAt,
                Email = user.Email,
                Id = user.Id,
                IsActivated = user.IsActivated,
                IsBlocked = user.IsBlocked,
                Roles = user.Roles,
                PreferredLanguage = user.PreferredLanguage,
                AvatarUrl = user.AvatarUrl
            };
        }

        private static UserGettedDto MapToGetDto(User user)
        {
            return new UserGettedDto
            {
                CreatedAt = user.CreatedAt,
                Email = user.Email,
                Id = user.Id,
                IsActivated = user.IsActivated,
                IsBlocked = user.IsBlocked,
                Roles = user.Roles,
                PreferredLanguage = user.PreferredLanguage,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl
            };
        }

        private static UserUpdatedDto MapToUpdatedDto(User user)
        {
            return new UserUpdatedDto
            {
                CreatedAt = user.CreatedAt,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UpdatedAt = user.UpdatedAt,
                PreferredLanguage = user.PreferredLanguage,
                IsActivated = user.IsActivated,
                Id = user.Id,
                IsBlocked = user.IsBlocked,
                Roles = user.Roles,
                AvatarUrl = user.AvatarUrl
            };
        }

        private void PrepareEmailConfirmation(User user, out string confirmationToken)
        {
            DateTime now = DateTime.UtcNow;
            confirmationToken = localAccountTokenService.GenerateToken();
            user.EmailConfirmationTokenHash = localAccountTokenService.ComputeHash(confirmationToken);
            user.EmailConfirmationTokenExpiresAt = now.AddHours(localAuthenticationSettings.EmailConfirmationTokenExpirationHours);
            user.EmailConfirmationSentAt = now;
            user.UpdatedAt = now;
        }

        private void PreparePasswordReset(User user, out string resetToken)
        {
            DateTime now = DateTime.UtcNow;
            resetToken = localAccountTokenService.GenerateToken();
            user.PasswordResetTokenHash = localAccountTokenService.ComputeHash(resetToken);
            user.PasswordResetTokenExpiresAt = now.AddMinutes(localAuthenticationSettings.PasswordResetTokenExpirationMinutes);
            user.PasswordResetSentAt = now;
            user.UpdatedAt = now;
        }

        private static void ClearEmailConfirmation(User user)
        {
            user.EmailConfirmationTokenHash = null;
            user.EmailConfirmationTokenExpiresAt = null;
            user.EmailConfirmationSentAt = null;
        }

        private static void ClearPasswordReset(User user)
        {
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.PasswordResetSentAt = null;
        }

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static bool IsValidPassword(string password)
        {
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
            return Regex.IsMatch(password ?? string.Empty, passwordPattern);
        }

        private static string? NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();
        }
    }
}
