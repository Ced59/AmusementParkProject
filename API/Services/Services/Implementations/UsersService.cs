using System.Security.Claims;
using System.Text.RegularExpressions;
using Amazon.SecurityToken.Model;
using Common.Users;
using Dtos.Pagination;
using Dtos.Users.ChangePassword;
using Dtos.Users.Creating;
using Dtos.Users.LockUser;
using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Dtos.Users.Roles;
using Dtos.Users.Updating;
using Dtos.Users.UserGet;
using Dtos.Users.Users;
using Entities.Model.Errors;
using Entities.Model.Users;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Settings;
using Services.Security;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations;

public class UsersService : IUsersService
{
    private readonly IJwtSettings _jwtSettings;
    private readonly IUserQueryHandler _userQueryHandler;

    public UsersService(IUserQueryHandler userQueryHandler, IJwtSettings jwtSettings)
    {
        _userQueryHandler = userQueryHandler;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    ///     Create User
    /// </summary>
    /// <param name="user">User to create</param>
    /// <returns>Confirmation created or error</returns>
    public async Task<OneOf<UserCreatedDto, ErrorDetail>>? CreateUserAsync(UserCreateDto user)
    {
        if (user.Password != user.VerifyPassword) return PasswordsNotSames;

        if (!IsValidEmail(user.Email)) return InvalidEmailAddress;

        if (await _userQueryHandler.ExistsByEmailAsync(user.Email)) return UserAlreadyExists;

        if (!IsValidPassword(user.Password)) return InvalidPassword;

        string? hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

        User userToCreate = new()
        {
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

        User? userCreated = await _userQueryHandler.CreateUserAsync(userToCreate);

        return new UserCreatedDto
        {
            CreatedAt = userCreated.CreatedAt,
            Email = userCreated.Email,
            Id = userCreated.Id,
            IsActivated = userCreated.IsActivated,
            IsBlocked = userCreated.IsBlocked,
            Roles = userCreated.Roles,
            PreferredLanguage = userCreated.PreferredLanguage,
            AvatarUrl = userCreated.AvatarUrl
        };
    }


    /// <summary>
    ///     Get user by email
    /// </summary>
    /// <param name="email">Email of user</param>
    /// <returns>User or error</returns>
    public async Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByEmailAsync(string email)
    {
        User? user = await _userQueryHandler.GetUserByEmailAsync(email);
        if (user == null) return UserNotExists;

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

    /// <summary>
    ///     Get user by id
    /// </summary>
    /// <param name="id">Id of user</param>
    /// <returns>User or error</returns>
    public async Task<OneOf<UserGettedDto, ErrorDetail>> GetUserByIdAsync(string id)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(id);

        if (user == null) return UserNotExists;

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

    /// <summary>
    ///     Update user
    /// </summary>
    /// <param name="id">Id of user</param>
    /// <param name="userUpdate">User updated</param>
    /// <returns>User updated or error</returns>
    public async Task<OneOf<UserUpdatedDto, ErrorDetail>> UpdateUserAsync(string id, UserUpdateDto userUpdate)
    {
        if (!IsValidEmail(userUpdate.Email) || (userUpdate.NewEmail != null && !IsValidEmail(userUpdate.NewEmail)))
            return InvalidEmailAddress;

        User? userToUpdate = await _userQueryHandler.GetUserByIdAsync(id);
        if (userToUpdate == null) return UserNotExists;

        if (userUpdate.NewEmail != null && userUpdate.Email != userUpdate.NewEmail)
        {
            User? existingUser = await _userQueryHandler.GetUserByEmailAsync(userUpdate.NewEmail);
            if (existingUser != null) return UserUpdateFailed;  

            userToUpdate.IsActivated = false;
            userToUpdate.Email = userUpdate.NewEmail;  
        }

        userToUpdate.LastName = userUpdate.LastName;
        userToUpdate.FirstName = userUpdate.FirstName;
        userToUpdate.UpdatedAt = DateTime.UtcNow;
        userToUpdate.PreferredLanguage = userUpdate.PreferredLanguage;
        userToUpdate.LastActivity = DateTime.UtcNow;
        userToUpdate.AvatarUrl = userUpdate.AvatarUrl;

        User? userUpdated = await _userQueryHandler.UpdateUserAsync(userToUpdate);
        if (userUpdated == null) return UserUpdateFailed;

        UserUpdatedDto userUpdatedDto = new()
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
    ///     Login
    /// </summary>
    /// <param name="credentials">Credentials to login</param>
    /// <returns>Jwt token or error</returns>
    public async Task<OneOf<UserLoggedDto, ErrorDetail>> LoginAsync(UserLoginDto credentials)
    {
        User? user = await _userQueryHandler.GetUserByEmailAsync(credentials.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(credentials.Password, user.HashedPassword)) return LoginFailed;

        string token = JwtHelper.GenerateToken(user, _jwtSettings);

        await _userQueryHandler.UpdateLastLoginAndActivityAsync(user.Id);

        return new UserLoggedDto { Token = token };
    }


    /// <summary>
    ///     Login by external providers
    /// </summary>
    /// <param name="email">Email of user</param>
    /// <returns>Jwt token or error</returns>
    public async Task<OneOf<UserLoggedDto, ErrorDetail>> LoginExternalAsync(string email)
    {
        User? user = await _userQueryHandler.GetUserByEmailAsync(email);
        if (user == null) return LoginFailed;

        string token = JwtHelper.GenerateToken(user, _jwtSettings);

        await _userQueryHandler.UpdateLastLoginAndActivityAsync(user.Id);

        return new UserLoggedDto { Token = token };
    }


    public async Task<OneOf<UserLoggedDto, ErrorDetail>>? CreateUserByInfosAsync(UserSocialCreate user)
    {
        User userToCreate = new()
        {
            Email = user.Email,
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
            HashedPassword = ""
        };

        User? userCreated = await _userQueryHandler.CreateUserAsync(userToCreate);

        if (userCreated == null) return LoginFailed;

        User? userLogin = await _userQueryHandler.GetUserByEmailAsync(userCreated.Email);
        if (userLogin == null) return LoginFailed;

        string token = JwtHelper.GenerateToken(userLogin, _jwtSettings);

        await _userQueryHandler.UpdateLastLoginAndActivityAsync(userLogin.Id);

        return new UserLoggedDto { Token = token };

    }


    /// <summary>
    ///     Refresh token
    /// </summary>
    /// <param name="token">Token to refresh</param>
    /// <returns>Token refreshed or error</returns>
    public async Task<OneOf<RefreshTokenResponseDto, ErrorDetail>> RefreshTokenAsync(RefreshTokenRequestDto token)
    {
        JwtHelper.ValidationResult result = JwtHelper.ValidateToken(token.RefreshToken, false, _jwtSettings);

        if (result.IsValid)
        {
            Claim? idUser = result.Token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
            User? user = await _userQueryHandler.GetUserByIdAsync(idUser.Value);

            if (user is null) return TokenRefreshFailed;

            int refreshLimit = _jwtSettings.TokenRefreshLimitMinutes;

            DateTime expireAt = user.LastActivity + TimeSpan.FromMinutes(refreshLimit);

            if (DateTime.UtcNow <= expireAt)
                return new RefreshTokenResponseDto
                {
                    RefreshToken = JwtHelper.GenerateToken(user, _jwtSettings)
                };

            return TokenRefreshFailedInactivity;
        }

        return TokenRefreshFailed;
    }

    /// <summary>
    ///     Assign role to User
    /// </summary>
    /// <param name="userId">Id user</param>
    /// <param name="roleToAssign">Role to assign</param>
    /// <returns>All roles of user or error</returns>
    public async Task<OneOf<RoleAssignedDto, ErrorDetail>> AssignRoleAsync(string userId, RoleAssignDto roleToAssign)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(userId);

        if (user == null) return UserNotExists;

        if (user.Roles.Contains(roleToAssign.Role)) return RoleAlreadyAssigned;

        User? updatedUser = await _userQueryHandler.AssignRoleAsync(userId, roleToAssign.Role);

        if (updatedUser is null) return AssignRoleFailed;

        return new RoleAssignedDto
        {
            UserId = updatedUser.Id,
            Roles = updatedUser.Roles
        };
    }


    /// <summary>
    ///     Remove role from User
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleToRemove">Role to remove</param>
    /// <returns>All roles of user or error</returns>
    public async Task<OneOf<RoleRemovedDto, ErrorDetail>> RemoveRoleAsync(string userId, RoleRemoveDto roleToRemove)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(userId);

        if (user == null) return UserNotExists;

        if (!user.Roles.Contains(roleToRemove.Role)) return RoleNotAssigned;

        User? updatedUser = await _userQueryHandler.RemoveRoleAsync(userId, roleToRemove.Role);

        if (updatedUser is null) return RemoveRoleFailed;

        return new RoleRemovedDto
        {
            UserId = updatedUser.Id,
            Roles = updatedUser.Roles
        };
    }


    /// <summary>
    ///     Lock user
    /// </summary>
    /// <param name="userToLock">User to lock</param>
    /// <returns>User locked</returns>
    public async Task<OneOf<UserLockedDto, ErrorDetail>> LockUser(UserToLockDto userToLock)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(userToLock.IdUser);

        if (user == null) return UserNotExists;

        User? userLocked = await _userQueryHandler.LockUser(userToLock.IdUser);

        if (userLocked is null) return CannotLockUser;

        return new UserLockedDto
        {
            FirstName = userLocked.FirstName,
            LastName = userLocked.LastName,
            UserId = userLocked.Id
        };
    }

    /// <summary>
    ///     Unlock user
    /// </summary>
    /// <param name="userToUnlock">User to unlock</param>
    /// <returns>User unlocked</returns>
    public async Task<OneOf<UserUnlockedDto, ErrorDetail>> UnlockUser(UserToUnlockDto userToUnlock)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(userToUnlock.IdUser);

        if (user == null) return UserNotExists;

        User? userLocked = await _userQueryHandler.UnlockUser(userToUnlock.IdUser);

        if (userLocked is null) return CannotUnlockUser;

        return new UserUnlockedDto
        {
            FirstName = userLocked.FirstName,
            LastName = userLocked.LastName,
            UserId = userLocked.Id
        };
    }

    /// <summary>
    ///     Get list of Users
    /// </summary>
    /// <param name="page">Number of page got in pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>Get list of users</returns>
    public async Task<(IEnumerable<UserDto>, PaginationDto)> GetAllUsersPaginatedAsync(int page, int pageSize)
    {
        long totalItems = await _userQueryHandler.GetTotalUsersCountAsync();
        IEnumerable<User> users = await _userQueryHandler.GetUsersPaginatedAsync(page, pageSize);

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

    /// <summary>
    ///     Change user password
    /// </summary>
    /// <param name="idUser">User to change password</param>
    /// <param name="changePasswordDto">OldPassword and/or new password & confirmation</param>
    /// <param name="isSelfChanged">The change password process id for the user who demands</param>
    /// <returns></returns>
    public async Task<OneOf<PasswordChangedDto, ErrorDetail>> ChangeUserPassword(string idUser, ChangePasswordDto changePasswordDto, bool isSelfChanged)
    {
        User? user = await _userQueryHandler.GetUserByIdAsync(idUser);

        if (user == null)
        {
            return UserNotExists;
        }

        if (isSelfChanged && !BCrypt.Net.BCrypt.Verify(changePasswordDto.ActualPassword, user.HashedPassword))
        {
            return IncorrectPassword;
        }

        string? hashedPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);

        bool changePasswordOk = await _userQueryHandler.ChangePassword(idUser, hashedPassword);

        if (changePasswordOk)
        {
            return new PasswordChangedDto
            {
                Message = $"Password of user {idUser} successfully changed"
            };
        }

        return ChangePasswordFailed;
    }


    /// <summary>
    ///     Validation email
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
    ///     Validation password
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True or false</returns>
    private bool IsValidPassword(string password)
    {
        // Le mot de passe doit contenir au moins 8 caractères, dont une majuscule, un chiffre et un caractère spécial
        string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
        return Regex.IsMatch(password, passwordPattern);
    }

    /// <summary>
    /// Telecharge l'avatar et l'enregistre dans l'API.
    /// </summary>
    /// <param name="imageUrl">URL source de l'image.</param>
    /// <param name="userId">Id du User.</param>
    /// <returns>Chemin de l'image dans l'API.</returns>
    public async Task<string> DownloadAndSaveUserAvatar(string imageUrl, string userId)
    {
        try
        {
            using HttpClient httpClient = new();
            byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            if (imageBytes.Length == 0)
                return "";

            string fileName = $"{userId}.jpg"; // On nomme le fichier avec l'ID utilisateur
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/user-avatar/", fileName);

            await File.WriteAllBytesAsync(savePath, imageBytes);

            return $"/images/user-avatar/{fileName}"; // Retourne le chemin relatif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du téléchargement de l'avatar : {ex.Message}");
            return "";
        }
    }


}