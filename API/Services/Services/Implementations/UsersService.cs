using System.Security.Claims;
using System.Text.RegularExpressions;
using Common.Users;
using Dtos.Users.Creating;
using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Dtos.Users.Updating;
using Entities.Model.Errors;
using Entities.Model.Users;
using OneOf;
using OneOf.Types;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Security;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations
{
    public class UsersService : IUsersService
    {
        private readonly IUserQueryHandler _userQueryHandler;
        private readonly IJwtSettings _jwtSettings;

        public UsersService(IUserQueryHandler userQueryHandler, IJwtSettings jwtSettings)
        {
            _userQueryHandler = userQueryHandler;
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Create User
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>Confirmation created or error</returns>
        public async Task<OneOf<UserCreatedDto, ErrorDetail>>? CreateUserAsync(UserCreateDto user)
        {
            if (user.Password != user.VerifyPassword)
            {
                return ErrorCodes.PasswordsNotSames;
            }

            if (!IsValidEmail(user.Email))
            {
                return ErrorCodes.InvalidEmailAddress;
            }

            if (await _userQueryHandler.ExistsByEmailAsync(user.Email))
            {
                return ErrorCodes.UserAlreadyExists;
            }

            if (!IsValidPassword(user.Password))
            {
                return ErrorCodes.InvalidPassword; 
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            var userToCreate = new User()
            {
                Id = Guid.NewGuid().ToString(),
                Email = user.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PreferredLanguage = user.PreferredLanguage,
                IsActivated = false,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.USER
                },
                HashedPassword = hashedPassword,
                LastLogin = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            var userCreated =  await _userQueryHandler.CreateUserAsync(userToCreate);

            return new UserCreatedDto
            {
                CreatedAt = userCreated.CreatedAt,
                Email = userCreated.Email,
                Id = userCreated.Id,
                IsActivated = userCreated.IsActivated,
                IsBlocked = userCreated.IsBlocked,
                Roles = userCreated.Roles,
                PreferredLanguage = userCreated.PreferredLanguage
            };
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email of user</param>
        /// <returns>User or error</returns>
        public async Task<OneOf<UserCreatedDto, ErrorDetail>> GetUserByEmailAsync(string email)
        {
            var user = await _userQueryHandler.GetUserByEmailAsync(email);
            if (user == null)
            {
                return ErrorCodes.UserNotExists;
            }

            return new UserCreatedDto
            {
                CreatedAt = user.CreatedAt,
                Email = user.Email,
                Id = user.Id,
                IsActivated = user.IsActivated,
                IsBlocked = user.IsBlocked,
                Roles = user.Roles,
                PreferredLanguage = user.PreferredLanguage
            };
        }

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="id">Id of user</param>
        /// <returns>User or error</returns>
        public async Task<OneOf<UserCreatedDto, ErrorDetail>> GetUserByIdAsync(string id)
        {
            var user = await _userQueryHandler.GetUserByIdAsync(id);

            if (user == null)
            {
                return ErrorCodes.UserNotExists;
            }

            return new UserCreatedDto
            {
                CreatedAt = user.CreatedAt,
                Email = user.Email,
                Id = user.Id,
                IsActivated = user.IsActivated,
                IsBlocked = user.IsBlocked,
                Roles = user.Roles,
                PreferredLanguage = user.PreferredLanguage
            };
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="id">Id of user</param>
        /// <param name="userUpdate">User updated</param>
        /// <returns>User updated or error</returns>
        public async Task<OneOf<UserUpdatedDto, ErrorDetail>> UpdateUser(string id, UserUpdateDto userUpdate)
        {
            if (!IsValidEmail(userUpdate.Email))
            {
                return ErrorCodes.InvalidEmailAddress;
            }

            var userToUpdate = await _userQueryHandler.GetUserByIdAsync(id);

            if (userToUpdate == null)
            {
                return ErrorCodes.UserNotExists;
            }

            userToUpdate.Email = userUpdate.Email;
            userToUpdate.LastName = userUpdate.LastName;
            userToUpdate.FirstName = userUpdate.FirstName;
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            userToUpdate.PreferredLanguage = userUpdate.PreferredLanguage;
            userToUpdate.LastActivity = DateTime.UtcNow;

            var userUpdated = await _userQueryHandler.UpdateUserAsync(userToUpdate);

            if (userUpdated == null)
            {
                return ErrorCodes.UserUpdateFailed;
            }

            var userUpdatedDto = new UserUpdatedDto
            {
                CreatedAt = userUpdated.CreatedAt,
                Email = userUpdated.Email,
                FirstName = userUpdated.FirstName,
                LastName = userUpdated.LastName,
                UpdatedAt = userUpdated.UpdatedAt,
                PreferredLanguage = userUpdated.PreferredLanguage,
                IsActivated = userUpdated.IsActivated,
                Id = userUpdated.Id,
                IsBlocked = userUpdated.IsBlocked,
                Roles = userUpdated.Roles
            };

            return userUpdatedDto;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="credentials">Credentials to login</param>
        /// <returns>Jwt token or error</returns>
        public async Task<OneOf<UserLoggedDto, ErrorDetail>> LoginAsync(UserLoginDto credentials)
        {
            var user = await _userQueryHandler.GetUserByEmailAsync(credentials.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(credentials.Password, user.HashedPassword))
            {
                return ErrorCodes.LoginFailed; 
            }

            var token = JwtHelper.GenerateToken(user, _jwtSettings);

            await _userQueryHandler.UpdateLastLoginAndActivityAsync(user.Id);

            return new UserLoggedDto { Token = token };
        }


        /// <summary>
        /// Refresh token
        /// </summary>
        /// <param name="token">Token to refresh</param>
        /// <returns>Token refreshed or error</returns>
        public async Task<OneOf<RefreshTokenResponseDto, ErrorDetail>> RefreshTokenAsync(RefreshTokenRequestDto token)
        {
            var result = JwtHelper.ValidateToken(token.RefreshToken, false, _jwtSettings);

            if (result.IsValid)
            {
                var idUser = result.Token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
                var user = await _userQueryHandler.GetUserByIdAsync(idUser.Value);

                if (user is null)
                {
                    return ErrorCodes.TokenRefreshFailed;
                }

                var refreshLimit = _jwtSettings.TokenRefreshLimitMinutes;

                var expireAt = user.LastActivity + TimeSpan.FromMinutes(refreshLimit);

                if (DateTime.UtcNow <= expireAt)
                {
                    return new RefreshTokenResponseDto
                    {
                        RefreshToken = JwtHelper.GenerateToken(user, _jwtSettings)
                    };
                }

                return ErrorCodes.TokenRefreshFailedInactivity;

            }

            return ErrorCodes.TokenRefreshFailed;
        }



        /// <summary>
        /// Validation email
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True or false</returns>
        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validation password
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True or false</returns>
        private bool IsValidPassword(string password)
        {
            // Le mot de passe doit contenir au moins 8 caractères, dont une majuscule, un chiffre et un caractère spécial
            var passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
            return Regex.IsMatch(password, passwordPattern);
        }
    }
}
