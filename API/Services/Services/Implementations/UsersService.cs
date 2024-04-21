using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Entities.Model.Errors;
using Entities.Model.Users;
using Entities.Model.Users.Enums;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations
{
    public class UsersService : IUsersService
    {
        private readonly IUserQueryHandler _userQueryHandler;

        public UsersService(IUserQueryHandler userQueryHandler)
        {
            _userQueryHandler = userQueryHandler;
        }

        public async Task<OneOf<UserCreated, ErrorDetail>>? CreateUserAsync(UserCreate user)
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

            var userToCreate = new UserInDb()
            {
                Id = Guid.NewGuid().ToString(),
                Email = user.Email,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                PreferredLanguage = user.PreferredLanguage,
                IsActivated = false,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.USER
                },
                HashedPassword = hashedPassword,
                LastLogin = DateTime.Now,
                LastActivity = DateTime.Now
            };

            var userCreated =  await _userQueryHandler.CreateUserAsync(userToCreate);

            return new UserCreated
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

        public async Task<OneOf<UserCreated, ErrorDetail>> GetUserByEmailAsync(string email)
        {
            var user = await _userQueryHandler.GetUserByEmailAsync(email);
            if (user == null)
            {
                return ErrorCodes.UserNotExists;
            }

            return new UserCreated
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

        public async Task<OneOf<UserCreated, ErrorDetail>> GetUserByIdAsync(string id)
        {
            var user = await _userQueryHandler.GetUserByIdAsync(id);

            if (user == null)
            {
                return ErrorCodes.UserNotExists;
            }

            return new UserCreated
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

        private bool IsValidPassword(string password)
        {
            // Le mot de passe doit contenir au moins 8 caractères, dont un chiffre et un caractère spécial
            var passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
            return Regex.IsMatch(password, passwordPattern);
        }

    }
}
